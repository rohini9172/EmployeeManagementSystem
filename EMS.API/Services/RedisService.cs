using System.Text.Json;
using StackExchange.Redis;

namespace EMS.API.Services;

// IRedisService defines the contract so we can mock Redis in unit tests
public interface IRedisService
{
    // ── String operations (cache-aside) ────────────────────────────────────
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task DeleteAsync(params string[] keys);
    Task<bool> ExistsAsync(string key);

    // ── Hash operations (employee activity summary) ────────────────────────
    Task HashSetAsync(string key, string field, string value);
    Task<string?> HashGetAsync(string key, string field);
    Task HashIncrementAsync(string key, string field, long by = 1);

    // ── List operations (recently viewed) ─────────────────────────────────
    Task ListPushAsync(string key, string value, int maxLength);
    Task<List<string>> ListGetAsync(string key);

    // ── Set operations (unique views) ──────────────────────────────────────
    Task SetAddAsync(string key, string value);
    Task<bool> SetContainsAsync(string key, string value);
    Task<long> SetLengthAsync(string key);

    // ── Sorted Set operations (popular employees) ──────────────────────────
    Task SortedSetIncrementAsync(string key, string member, double score = 1);
    Task<List<(string Member, double Score)>> SortedSetGetTopAsync(string key, int count);

    // ── Atomic counter ─────────────────────────────────────────────────────
    Task<long> IncrementAsync(string key);

    // ── Pub/Sub ────────────────────────────────────────────────────────────
    Task PublishAsync(string channel, string message);
}

public class RedisService : IRedisService
{
    private readonly IDatabase _db;
    private readonly IConnectionMultiplexer _connection;
    private readonly ILogger<RedisService> _logger;
    private readonly TimeSpan _defaultTtl;

    public RedisService(
        IConnectionMultiplexer connection,
        IConfiguration config,
        ILogger<RedisService> logger)
    {
        _connection = connection;
        // GetDatabase() is cheap — it does NOT open a new connection.
        // The ConnectionMultiplexer (singleton) manages the actual TCP connection.
        _db = connection.GetDatabase();
        _logger = logger;

        // Read TTL from config — defaults to 5 minutes if not set
        var ttlMinutes = config.GetValue<int>("Redis:EmployeeCacheTtlMinutes", 5);
        _defaultTtl = TimeSpan.FromMinutes(ttlMinutes);
    }

    // ── String: Get ────────────────────────────────────────────────────────
    // Returns null on cache miss OR if Redis is unavailable (graceful fallback)
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            if (value.IsNullOrEmpty) return default; // Cache miss

            // Deserialize the JSON string back to the original type
            return JsonSerializer.Deserialize<T>(value!);
        }
        catch (Exception ex)
        {
            // Log but don't throw — caller will fall back to SQL Server
            _logger.LogWarning(ex, "Redis GET failed for key {Key}", key);
            return default;
        }
    }

    // ── String: Set ────────────────────────────────────────────────────────
    // Serializes the value to JSON and stores it with a TTL.
    // TTL = Time To Live: Redis automatically deletes the key after this duration.
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            // Use provided expiry or fall back to the configured default TTL
            await _db.StringSetAsync(key, json, expiry ?? _defaultTtl);
        }
        catch (Exception ex)
        {
            // Cache write failure is non-fatal — data is still in SQL Server
            _logger.LogWarning(ex, "Redis SET failed for key {Key}", key);
        }
    }

    // ── String: Delete (cache invalidation) ───────────────────────────────
    // Deletes one or more keys atomically.
    // Called after Create/Update/Delete so stale data is never served.
    public async Task DeleteAsync(params string[] keys)
    {
        try
        {
            // Convert string keys to RedisKey array for the batch delete
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            await _db.KeyDeleteAsync(redisKeys);
        }
        catch (Exception ex)
        {
            // IMPORTANT: We log but do NOT re-throw.
            // The database write already succeeded — we must not roll it back
            // just because Redis cache invalidation failed.
            _logger.LogWarning(ex, "Redis DELETE failed for keys {Keys}", string.Join(", ", keys));
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try { return await _db.KeyExistsAsync(key); }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis EXISTS failed for key {Key}", key); return false; }
    }

    // ── Hash: Set field ────────────────────────────────────────────────────
    // Redis Hash = like a dictionary inside a single key.
    // Used for employee activity summary: { "viewCount": "42", "lastViewed": "..." }
    public async Task HashSetAsync(string key, string field, string value)
    {
        try { await _db.HashSetAsync(key, field, value); }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis HSET failed for key {Key}", key); }
    }

    public async Task<string?> HashGetAsync(string key, string field)
    {
        try
        {
            var value = await _db.HashGetAsync(key, field);
            return value.IsNullOrEmpty ? null : value.ToString();
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis HGET failed for key {Key}", key); return null; }
    }

    // ── Hash: Atomic increment ─────────────────────────────────────────────
    // HINCRBY is atomic — safe when multiple users view the same employee simultaneously.
    // No race condition: Redis processes commands one at a time (single-threaded).
    public async Task HashIncrementAsync(string key, string field, long by = 1)
    {
        try { await _db.HashIncrementAsync(key, field, by); }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis HINCRBY failed for key {Key}", key); }
    }

    // ── List: Push with max length enforcement ─────────────────────────────
    // Used for "recently viewed" — keeps only the last N employee IDs.
    // LPUSH adds to the front; LTRIM removes from the back to enforce max length.
    public async Task ListPushAsync(string key, string value, int maxLength)
    {
        try
        {
            // Remove existing occurrence first to prevent duplicates in the list
            await _db.ListRemoveAsync(key, value);
            // Push new value to the front (most recent first)
            await _db.ListLeftPushAsync(key, value);
            // Trim to keep only the most recent `maxLength` items
            await _db.ListTrimAsync(key, 0, maxLength - 1);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis LPUSH failed for key {Key}", key); }
    }

    public async Task<List<string>> ListGetAsync(string key)
    {
        try
        {
            var values = await _db.ListRangeAsync(key);
            return values.Select(v => v.ToString()).ToList();
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis LRANGE failed for key {Key}", key); return []; }
    }

    // ── Set: Add member ────────────────────────────────────────────────────
    // Redis Set = unordered collection of unique strings.
    // SADD ignores duplicates automatically — perfect for unique viewer tracking.
    public async Task SetAddAsync(string key, string value)
    {
        try { await _db.SetAddAsync(key, value); }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis SADD failed for key {Key}", key); }
    }

    public async Task<bool> SetContainsAsync(string key, string value)
    {
        try { return await _db.SetContainsAsync(key, value); }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis SISMEMBER failed for key {Key}", key); return false; }
    }

    public async Task<long> SetLengthAsync(string key)
    {
        try { return await _db.SetLengthAsync(key); }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis SCARD failed for key {Key}", key); return 0; }
    }

    // ── Sorted Set: Increment score ────────────────────────────────────────
    // Redis Sorted Set = members ranked by a numeric score.
    // ZINCRBY atomically increments the score — used for view count rankings.
    public async Task SortedSetIncrementAsync(string key, string member, double score = 1)
    {
        try { await _db.SortedSetIncrementAsync(key, member, score); }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis ZINCRBY failed for key {Key}", key); }
    }

    // Returns top N members ordered by score descending (highest views first)
    public async Task<List<(string Member, double Score)>> SortedSetGetTopAsync(string key, int count)
    {
        try
        {
            var entries = await _db.SortedSetRangeByRankWithScoresAsync(
                key, 0, count - 1, Order.Descending);
            return entries.Select(e => (e.Element.ToString(), e.Score)).ToList();
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis ZRANGE failed for key {Key}", key); return []; }
    }

    // ── Atomic counter (INCR) ──────────────────────────────────────────────
    // Redis INCR is atomic — guaranteed no race condition even with 1000 concurrent users.
    // Compare to SQL: UPDATE counter SET value = value + 1 requires a transaction.
    public async Task<long> IncrementAsync(string key)
    {
        try { return await _db.StringIncrementAsync(key); }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis INCR failed for key {Key}", key); return 0; }
    }

    // ── Pub/Sub: Publish ───────────────────────────────────────────────────
    // Publishes a message to a channel — all subscribers receive it instantly.
    // Used in Phase 7 to notify when an employee is updated.
    public async Task PublishAsync(string channel, string message)
    {
        try
        {
            var subscriber = _connection.GetSubscriber();
            await subscriber.PublishAsync(RedisChannel.Literal(channel), message);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis PUBLISH failed for channel {Channel}", channel); }
    }
}

using EMS.API.DTOs;
using EMS.API.Repositories;

namespace EMS.API.Services;

public interface IActivityService
{
    Task RecordViewAsync(int viewerUserId, int viewedEmployeeId);
    Task<List<RecentlyViewedResponse>> GetRecentlyViewedAsync(int userId);
    Task<List<PopularEmployeeResponse>> GetPopularEmployeesAsync(int count = 5);
    Task<ActivitySummaryResponse> GetSummaryAsync();
    Task<EmployeeActivityStats> GetEmployeeStatsAsync(int employeeId);
}

public class ActivityService(
    IRedisService redis,
    IEmployeeRepository employeeRepo) : IActivityService
{
    // ── Record a Profile View ──────────────────────────────────────────────
    // Called every time GET /api/employees/{id} is hit.
    // Updates FIVE Redis data structures in one operation:
    //   1. String INCR  — atomic global view counter for this employee
    //   2. Hash HINCRBY — increments viewCount field in employee's activity hash
    //   3. Hash HSET    — updates lastViewed timestamp in employee's activity hash
    //   4. List LPUSH   — prepends employee ID to viewer's recently-viewed list
    //   5. Set SADD     — adds employee ID to global unique-views set
    //   6. Sorted Set ZINCRBY — increments employee's score in popularity ranking
    public async Task RecordViewAsync(int viewerUserId, int viewedEmployeeId)
    {
        var empIdStr = viewedEmployeeId.ToString();
        var activityKey = CacheKeys.EmployeeActivityHash(viewedEmployeeId);

        // 1 & 2. Atomic increment — HINCRBY is safe under concurrent requests.
        //    If 100 users view the same employee simultaneously, each HINCRBY
        //    executes atomically. No two increments can overlap — Redis is single-threaded.
        await redis.HashIncrementAsync(activityKey, "viewCount");

        // 3. Record when this employee was last viewed (ISO 8601 UTC string)
        await redis.HashSetAsync(activityKey, "lastViewed", DateTime.UtcNow.ToString("o"));

        // 4. Add to this user's recently-viewed list (max 5 entries, no duplicates).
        //    LPUSH + LTRIM keeps only the 5 most recent, most-recent first.
        //    ListPushAsync removes duplicates before pushing so the same employee
        //    doesn't appear twice in the list.
        await redis.ListPushAsync(CacheKeys.RecentlyViewed(viewerUserId), empIdStr, maxLength: 5);

        // 5. Track unique employee IDs viewed globally.
        //    SADD ignores duplicates automatically — the set only grows when a
        //    new (never-before-viewed) employee is added.
        await redis.SetAddAsync(CacheKeys.UniqueViewsSet, empIdStr);

        // 6. Increment this employee's score in the popularity sorted set.
        //    ZINCRBY atomically adds 1 to the score. The sorted set auto-reorders
        //    by score so the most-viewed employee is always at the top.
        await redis.SortedSetIncrementAsync(CacheKeys.PopularEmployees, empIdStr);
    }

    // ── Recently Viewed (Redis List) ───────────────────────────────────────
    // Returns the last 5 employees viewed by this specific user.
    // The List stores employee IDs — we then fetch their details from cache or DB.
    // SECURITY: userId comes from JWT claims in the controller, never from the client.
    public async Task<List<RecentlyViewedResponse>> GetRecentlyViewedAsync(int userId)
    {
        // Get the list of employee IDs from Redis (stored as strings)
        var ids = await redis.ListGetAsync(CacheKeys.RecentlyViewed(userId));
        if (ids.Count == 0) return [];

        var result = new List<RecentlyViewedResponse>();
        foreach (var idStr in ids)
        {
            if (!int.TryParse(idStr, out var empId)) continue;

            // Try Redis cache first, then SQL Server on miss
            var emp = await redis.GetAsync<EmployeeResponse>(CacheKeys.Employee(empId))
                      ?? (await employeeRepo.GetByIdAsync(empId) is { } e
                          ? MapToResponse(e) : null);

            if (emp != null)
                result.Add(new RecentlyViewedResponse
                {
                    EmployeeId = emp.EmployeeId,
                    Name = emp.Name,
                    Department = emp.Department,
                    Designation = emp.Designation
                });
        }
        return result;
    }

    // ── Popular Employees (Redis Sorted Set) ───────────────────────────────
    // Returns top N employees ranked by view count.
    // ZRANGE with BYSCORE DESC gives us the highest-scored members first.
    public async Task<List<PopularEmployeeResponse>> GetPopularEmployeesAsync(int count = 5)
    {
        // Get top N members with their scores from the sorted set
        var entries = await redis.SortedSetGetTopAsync(CacheKeys.PopularEmployees, count);
        if (entries.Count == 0) return [];

        var result = new List<PopularEmployeeResponse>();
        foreach (var (member, score) in entries)
        {
            if (!int.TryParse(member, out var empId)) continue;

            // Resolve employee details from cache or DB
            var emp = await redis.GetAsync<EmployeeResponse>(CacheKeys.Employee(empId))
                      ?? (await employeeRepo.GetByIdAsync(empId) is { } e
                          ? MapToResponse(e) : null);

            if (emp != null)
                result.Add(new PopularEmployeeResponse
                {
                    EmployeeId = empId,
                    Name = emp.Name,
                    Department = emp.Department,
                    ViewCount = (long)score  // Score = total view count
                });
        }
        return result;
    }

    // ── Activity Summary (Admin only) ──────────────────────────────────────
    // Combines data from multiple Redis structures into one admin overview.
    public async Task<ActivitySummaryResponse> GetSummaryAsync()
    {
        // SCARD returns the number of unique employee IDs in the set
        var uniqueCount = await redis.SetLengthAsync(CacheKeys.UniqueViewsSet);
        var topEmployees = await GetPopularEmployeesAsync(10);

        return new ActivitySummaryResponse
        {
            TotalUniqueEmployeesViewed = uniqueCount,
            TopEmployees = topEmployees
        };
    }

    // ── Per-Employee Stats (Redis Hash) ────────────────────────────────────
    // Returns viewCount and lastViewed from the employee's activity hash.
    // Hash key: activity:employee:{id}
    // Hash fields: { "viewCount": "42", "lastViewed": "2024-01-15T10:30:00Z" }
    public async Task<EmployeeActivityStats> GetEmployeeStatsAsync(int employeeId)
    {
        var key = CacheKeys.EmployeeActivityHash(employeeId);

        // HGET fetches individual fields from the hash
        var viewCountStr = await redis.HashGetAsync(key, "viewCount");
        var lastViewed = await redis.HashGetAsync(key, "lastViewed");

        return new EmployeeActivityStats
        {
            EmployeeId = employeeId,
            ViewCount = long.TryParse(viewCountStr, out var vc) ? vc : 0,
            LastViewed = lastViewed ?? "Never"
        };
    }

    private static EmployeeResponse MapToResponse(Models.Employee e) => new()
    {
        EmployeeId = e.EmployeeId,
        UserId = e.UserId,
        Name = e.User.Name,
        Email = e.User.Email,
        Department = e.Department,
        Designation = e.Designation,
        Phone = e.Phone,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}

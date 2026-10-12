namespace EMS.API.Services;

// Centralising all Redis key patterns here means:
// - No magic strings scattered across services
// - Easy to change key format in one place
// - Self-documenting — you can see every Redis key the app uses at a glance
public static class CacheKeys
{
    // String / List cache — stores serialized employee data
    public const string AllEmployees = "employees:all";
    public static string Employee(int id) => $"employee:{id}";

    // Hash — stores activity summary per employee (view count, last viewed)
    public static string EmployeeActivityHash(int employeeId) => $"activity:employee:{employeeId}";

    // List — stores last 5 employee IDs viewed by a specific user
    public static string RecentlyViewed(int userId) => $"activity:recent:{userId}";

    // Set — tracks unique employee IDs viewed (used for unique viewer counts)
    public const string UniqueViewsSet = "activity:unique-views";

    // Sorted Set — ranks employees by total view count (score = view count)
    public const string PopularEmployees = "activity:popular";

    // Rate limiting — tracks request count per IP for search endpoint
    public static string RateLimit(string ip) => $"ratelimit:search:{ip}";
}

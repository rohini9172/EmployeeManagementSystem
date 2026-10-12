namespace EMS.API.DTOs;

// Returned by GET /api/activity/recent
// Shows the last 5 employees viewed by the currently logged-in user
public class RecentlyViewedResponse
{
    public int EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
}

// Returned by GET /api/activity/popular
// Shows employees ranked by total view count (from Redis Sorted Set)
public class PopularEmployeeResponse
{
    public int EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public long ViewCount { get; set; }
}

// Returned by GET /api/activity/summary (Admin only)
// Aggregates data from multiple Redis data structures
public class ActivitySummaryResponse
{
    public long TotalUniqueEmployeesViewed { get; set; }  // From Redis Set (SCARD)
    public List<PopularEmployeeResponse> TopEmployees { get; set; } = [];
}

// Returned by GET /api/activity/employee/{id}/stats
// Per-employee stats from Redis Hash
public class EmployeeActivityStats
{
    public int EmployeeId { get; set; }
    public long ViewCount { get; set; }       // From Hash field "viewCount"
    public string LastViewed { get; set; } = string.Empty; // From Hash field "lastViewed"
}

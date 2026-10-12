using System.Security.Claims;
using EMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/activity")]
[Authorize]
public class ActivityController(IActivityService activityService) : ControllerBase
{
    // GET /api/activity/recent
    // Returns the last 5 employees viewed by the currently logged-in user.
    // Data source: Redis List (activity:recent:{userId})
    // SECURITY: userId is read from JWT claims — a user can only see their own history.
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent()
    {
        // Extract userId from JWT — never accept it from query string or body
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await activityService.GetRecentlyViewedAsync(userId);
        return Ok(result);
    }

    // GET /api/activity/popular
    // Returns top 5 most-viewed employees ranked by view count.
    // Data source: Redis Sorted Set (activity:popular)
    // Available to both Admin and Employee roles
    [HttpGet("popular")]
    public async Task<IActionResult> GetPopular()
    {
        var result = await activityService.GetPopularEmployeesAsync(5);
        return Ok(result);
    }

    // GET /api/activity/summary
    // Returns admin-level overview: unique employees viewed + top 10 rankings.
    // Data source: Redis Set (SCARD) + Sorted Set (ZRANGE)
    [HttpGet("summary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSummary()
    {
        var result = await activityService.GetSummaryAsync();
        return Ok(result);
    }

    // GET /api/activity/employee/{id}/stats
    // Returns view count and last-viewed time for a specific employee.
    // Data source: Redis Hash (activity:employee:{id})
    [HttpGet("employee/{id:int}/stats")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetEmployeeStats(int id)
    {
        var result = await activityService.GetEmployeeStatsAsync(id);
        return Ok(result);
    }
}

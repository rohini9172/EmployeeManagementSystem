using System.Security.Claims;
using EMS.API.DTOs;
using EMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    // POST /api/auth/register
    // Public — no JWT required. Always assigns Employee role.
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request, ct);
        if (!result.Success)
            return Conflict(new { message = result.Error });

        return Ok(result.Data);
    }

    // POST /api/auth/login
    // Public — returns a JWT on success
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        if (result == null)
            // Return generic message — don't reveal whether email or password was wrong
            return Unauthorized(new { message = "Invalid email or password." });

        return Ok(result);
    }

    // GET /api/auth/me
    // Requires a valid JWT. Used by the React app on startup to verify the stored
    // token is still valid and get the latest user info (name, role).
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        // UserId comes from the JWT claim — never from the request body
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await authService.GetCurrentUserAsync(userId, ct);
        return user == null ? NotFound() : Ok(user);
    }

    // POST /api/auth/change-password
    // Any logged-in user can change their own password.
    // They must provide their current password to confirm identity.
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await authService.ChangePasswordAsync(userId, request, ct);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Password changed successfully." });
    }
}

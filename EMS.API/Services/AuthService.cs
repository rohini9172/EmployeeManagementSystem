using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EMS.API.DTOs;
using EMS.API.Models;
using EMS.API.Repositories;
using Microsoft.IdentityModel.Tokens;

namespace EMS.API.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<CurrentUserResponse?> GetCurrentUserAsync(int userId, CancellationToken ct = default);
    Task<ServiceResult<bool>> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default);
}

public class AuthService(
    IUserRepository userRepo,
    IEmployeeRepository employeeRepo,
    IConfiguration config) : IAuthService
{
    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await userRepo.GetByEmailAsync(request.Email, ct);
        if (user == null) return null;

        // BCrypt.Verify safely compares plain-text password against the stored hash
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) return null;

        return BuildAuthResponse(user);
    }

    public async Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await userRepo.EmailExistsAsync(request.Email, ct))
            return ServiceResult<AuthResponse>.Fail("Email already in use.");

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            // Always hash before storing — never store plain-text passwords
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "Employee" // Public registration can never self-assign Admin role
        };
        await userRepo.CreateAsync(user, ct);

        // Every registered user gets a linked Employee profile automatically
        var employee = new Employee
        {
            UserId = user.UserId,
            Department = string.Empty,
            Designation = string.Empty,
            Phone = string.Empty
        };
        await employeeRepo.CreateAsync(employee, ct);

        return ServiceResult<AuthResponse>.Ok(BuildAuthResponse(user));
    }

    // Called by GET /api/auth/me — lets the frontend verify the JWT is still valid
    // and refresh the user's display name/role without re-logging in
    public async Task<CurrentUserResponse?> GetCurrentUserAsync(int userId, CancellationToken ct = default)
    {
        var user = await userRepo.GetByIdAsync(userId, ct);
        if (user == null) return null;

        return new CurrentUserResponse
        {
            UserId = user.UserId,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role
        };
    }

    public async Task<ServiceResult<bool>> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await userRepo.GetByIdAsync(userId, ct);
        if (user == null) return ServiceResult<bool>.Fail("User not found.");

        // Verify the current password before allowing the change
        // This prevents someone who grabbed an unlocked screen from changing the password
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return ServiceResult<bool>.Fail("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await userRepo.UpdateAsync(user, ct);

        return ServiceResult<bool>.Ok(true);
    }

    // Builds a signed JWT token with user claims embedded inside it.
    // Claims are readable by the frontend (base64 decoded) but cannot be tampered
    // with because the signature would break — the server verifies the signature on every request.
    private string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(config["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key is not configured.")));

        var claims = new[]
        {
            // NameIdentifier = UserId — used in controllers to identify the caller
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            // Role claim — used by [Authorize(Roles = "Admin")] attribute
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(ClaimTypes.Name, user.Name)
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8), // Token valid for 8 hours
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private AuthResponse BuildAuthResponse(User user) => new()
    {
        Token = GenerateJwt(user),
        Role = user.Role,
        UserId = user.UserId,
        Name = user.Name,
        Email = user.Email
    };
}

using System.ComponentModel.DataAnnotations;

namespace EMS.API.DTOs;

// ── Auth DTOs ──────────────────────────────────────────────────────────────

public class RegisterRequest
{
    [Required] public string Name { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(6)] public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

// Returned to the client after a successful login
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// Returned by GET /api/auth/me — lets the frontend verify the token is still valid
public class CurrentUserResponse
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

// Used by POST /api/auth/change-password
public class ChangePasswordRequest
{
    [Required] public string CurrentPassword { get; set; } = string.Empty;
    [Required, MinLength(6)] public string NewPassword { get; set; } = string.Empty;
}

// ── Employee DTOs ──────────────────────────────────────────────────────────

// Used when Admin creates a new employee (includes login credentials)
public class CreateEmployeeRequest
{
    [Required] public string Name { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(6)] public string Password { get; set; } = string.Empty;
    [Required] public string Department { get; set; } = string.Empty;
    [Required] public string Designation { get; set; } = string.Empty;
    [Required] public string Phone { get; set; } = string.Empty;
}

// Used when updating an employee — Admin can update all fields
public class UpdateEmployeeRequest
{
    public string? Department { get; set; }
    public string? Designation { get; set; }
    public string? Phone { get; set; }
    public string? Name { get; set; }
}

// Safe response DTO — never exposes PasswordHash
public class EmployeeResponse
{
    public int EmployeeId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

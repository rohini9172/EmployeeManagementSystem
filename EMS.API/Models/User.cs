namespace EMS.API.Models;

// Represents a login account. One User can have one Employee profile.
public class User
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // BCrypt hash — never store plain text passwords
    public string PasswordHash { get; set; } = string.Empty;

    // "Admin" or "Employee" — used for role-based authorization
    public string Role { get; set; } = "Employee";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property: one User → one Employee profile
    public Employee? Employee { get; set; }
}

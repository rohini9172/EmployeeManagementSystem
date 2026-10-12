namespace EMS.API.Models;

// Stores professional details of an employee, linked to a User account.
public class Employee
{
    public int EmployeeId { get; set; }

    // Foreign key — links this profile to a User login account
    public int UserId { get; set; }

    public string Department { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property: back-reference to the User
    public User User { get; set; } = null!;
}

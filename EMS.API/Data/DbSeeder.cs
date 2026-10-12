using EMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EMS.API.Data;

// Runs on application startup to ensure the database has seed data.
// Using a seeder (instead of pure EF HasData) lets us hash passwords at runtime
// so we never store plain-text passwords in migrations.
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration config)
    {
        // Apply any pending migrations automatically on startup
        await db.Database.MigrateAsync();

        // ── Seed Admin ─────────────────────────────────────────────────────
        var adminEmail = config["Seed:AdminEmail"] ?? "admin@ems.com";
        var adminPassword = config["Seed:AdminPassword"] ?? "Admin@123";

        var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (admin == null)
        {
            admin = new User
            {
                Name = "Admin User",
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }
        else if (admin.PasswordHash == "PLACEHOLDER")
        {
            // Fix the placeholder hash inserted by EF migration seed
            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
            await db.SaveChangesAsync();
        }

        // ── Seed Sample Employees ──────────────────────────────────────────
        var sampleEmployees = new[]
        {
            new { Name = "Alice Johnson", Email = "alice@ems.com", Dept = "Engineering", Desig = "Software Engineer", Phone = "555-0101" },
            new { Name = "Bob Smith",     Email = "bob@ems.com",   Dept = "HR",          Desig = "HR Manager",        Phone = "555-0102" },
            new { Name = "Carol White",   Email = "carol@ems.com", Dept = "Finance",     Desig = "Accountant",        Phone = "555-0103" },
        };

        var defaultPassword = config["Seed:EmployeePassword"] ?? "Employee@123";

        foreach (var emp in sampleEmployees)
        {
            // Skip if user already exists
            if (await db.Users.AnyAsync(u => u.Email == emp.Email)) continue;

            var user = new User
            {
                Name = emp.Name,
                Email = emp.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
                Role = "Employee",
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(); // Save to get UserId

            var employee = new Employee
            {
                UserId = user.UserId,
                Department = emp.Dept,
                Designation = emp.Desig,
                Phone = emp.Phone,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Employees.Add(employee);
            await db.SaveChangesAsync();
        }
    }
}

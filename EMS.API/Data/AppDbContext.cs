using EMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EMS.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── User table constraints ─────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.UserId);
            e.HasIndex(u => u.Email).IsUnique(); // Email must be unique
            e.Property(u => u.Role).HasDefaultValue("Employee");
        });

        // ── Employee table constraints ─────────────────────────────────────
        modelBuilder.Entity<Employee>(e =>
        {
            e.HasKey(emp => emp.EmployeeId);

            // One User → One Employee (cascade delete: removing a user removes their profile)
            e.HasOne(emp => emp.User)
             .WithOne(u => u.Employee)
             .HasForeignKey<Employee>(emp => emp.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Seed Data ──────────────────────────────────────────────────────
        // Admin user — password is read from config at runtime via DbSeeder.
        // Here we seed a placeholder; DbSeeder overwrites it on startup.
        // We still need static seed IDs so EF migrations don't regenerate them.
        modelBuilder.Entity<User>().HasData(
            new User
            {
                UserId = 1,
                Name = "Admin User",
                Email = "admin@ems.com",
                // Placeholder hash — replaced by DbSeeder using config value
                PasswordHash = "PLACEHOLDER",
                Role = "Admin",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}

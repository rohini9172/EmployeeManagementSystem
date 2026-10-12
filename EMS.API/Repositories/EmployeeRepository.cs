using EMS.API.Data;
using EMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EMS.API.Repositories;

// Handles all SQL Server queries for employees.
// CancellationToken lets ASP.NET cancel DB queries if the HTTP request is aborted.
public class EmployeeRepository(AppDbContext db) : IEmployeeRepository
{
    public async Task<List<Employee>> GetAllAsync(CancellationToken ct = default)
        // Include User so we can access Name and Email in the response DTO
        => await db.Employees.Include(e => e.User).ToListAsync(ct);

    public async Task<Employee?> GetByIdAsync(int id, CancellationToken ct = default)
        => await db.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.EmployeeId == id, ct);

    public async Task<Employee?> GetByUserIdAsync(int userId, CancellationToken ct = default)
        => await db.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.UserId == userId, ct);

    public async Task<List<Employee>> SearchAsync(string term, CancellationToken ct = default)
    {
        var lower = term.ToLower();
        // Search across Name, Department, and Designation fields
        return await db.Employees
            .Include(e => e.User)
            .Where(e => e.User.Name.ToLower().Contains(lower)
                     || e.Department.ToLower().Contains(lower)
                     || e.Designation.ToLower().Contains(lower))
            .ToListAsync(ct);
    }

    public async Task<Employee> CreateAsync(Employee employee, CancellationToken ct = default)
    {
        db.Employees.Add(employee);
        await db.SaveChangesAsync(ct);
        return employee;
    }

    public async Task<Employee> UpdateAsync(Employee employee, CancellationToken ct = default)
    {
        employee.UpdatedAt = DateTime.UtcNow;
        db.Employees.Update(employee);
        await db.SaveChangesAsync(ct);
        return employee;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        // Deleting the User cascades to Employee due to OnDelete(Cascade) in DbContext
        var employee = await db.Employees.FindAsync([id], ct);
        if (employee != null)
        {
            var user = await db.Users.FindAsync([employee.UserId], ct);
            if (user != null) db.Users.Remove(user);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        => await db.Employees.AnyAsync(e => e.EmployeeId == id, ct);
}

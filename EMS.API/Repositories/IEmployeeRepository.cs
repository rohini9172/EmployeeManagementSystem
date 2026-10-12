using EMS.API.Models;

namespace EMS.API.Repositories;

// Defines the contract for all SQL Server employee data access.
// Using an interface allows easy unit testing by swapping with a mock.
public interface IEmployeeRepository
{
    Task<List<Employee>> GetAllAsync(CancellationToken ct = default);
    Task<Employee?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Employee?> GetByUserIdAsync(int userId, CancellationToken ct = default);
    Task<List<Employee>> SearchAsync(string term, CancellationToken ct = default);
    Task<Employee> CreateAsync(Employee employee, CancellationToken ct = default);
    Task<Employee> UpdateAsync(Employee employee, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
}

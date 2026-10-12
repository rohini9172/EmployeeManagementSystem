using EMS.API.DTOs;
using EMS.API.Models;
using EMS.API.Repositories;

namespace EMS.API.Services;

public interface IEmployeeService
{
    Task<List<EmployeeResponse>> GetAllAsync(CancellationToken ct = default);
    Task<EmployeeResponse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<EmployeeResponse?> GetByUserIdAsync(int userId, CancellationToken ct = default);
    Task<List<EmployeeResponse>> SearchAsync(string term, CancellationToken ct = default);
    Task<ServiceResult<EmployeeResponse>> CreateAsync(CreateEmployeeRequest request, CancellationToken ct = default);
    Task<EmployeeResponse?> UpdateAsync(int id, UpdateEmployeeRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

public class EmployeeService(
    IEmployeeRepository employeeRepo,
    IUserRepository userRepo,
    IRedisService redis) : IEmployeeService  // Redis injected here
{
    // ── GET ALL — Cache-Aside Pattern ──────────────────────────────────────
    // Step 1: Check Redis for cached employee list
    // Step 2: Cache hit → return immediately (no SQL query)
    // Step 3: Cache miss → query SQL Server, store result in Redis, return data
    public async Task<List<EmployeeResponse>> GetAllAsync(CancellationToken ct = default)
    {
        // Step 1: Try Redis first
        var cached = await redis.GetAsync<List<EmployeeResponse>>(CacheKeys.AllEmployees);
        if (cached != null)
        {
            // Cache HIT — data came from Redis, SQL Server was not touched
            return cached;
        }

        // Step 2: Cache MISS — go to SQL Server
        var employees = await employeeRepo.GetAllAsync(ct);
        var result = employees.Select(MapToResponse).ToList();

        // Step 3: Store in Redis with TTL so it auto-expires after 5 minutes
        // After TTL expires, the next request will be a cache miss and refresh from SQL
        await redis.SetAsync(CacheKeys.AllEmployees, result);

        return result;
    }

    // ── GET BY ID — Individual Employee Cache ─────────────────────────────
    public async Task<EmployeeResponse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        // Check Redis for this specific employee's cached data
        var cached = await redis.GetAsync<EmployeeResponse>(CacheKeys.Employee(id));
        if (cached != null) return cached; // Cache hit

        // Cache miss — query SQL Server
        var employee = await employeeRepo.GetByIdAsync(id, ct);
        if (employee == null) return null;

        var result = MapToResponse(employee);

        // Cache individual employee with TTL
        await redis.SetAsync(CacheKeys.Employee(id), result);

        return result;
    }

    // GetByUserId does NOT cache — it's used for the "me" endpoint which is
    // user-specific and changes less predictably. Simple SQL query is fine here.
    public async Task<EmployeeResponse?> GetByUserIdAsync(int userId, CancellationToken ct = default)
    {
        var employee = await employeeRepo.GetByUserIdAsync(userId, ct);
        return employee == null ? null : MapToResponse(employee);
    }

    // Search does NOT cache — search results vary by term and are rate-limited.
    // Caching search results would require a key per search term which is wasteful.
    public async Task<List<EmployeeResponse>> SearchAsync(string term, CancellationToken ct = default)
    {
        var employees = await employeeRepo.SearchAsync(term, ct);
        return employees.Select(MapToResponse).ToList();
    }

    // ── CREATE — Invalidate List Cache ────────────────────────────────────
    public async Task<ServiceResult<EmployeeResponse>> CreateAsync(CreateEmployeeRequest request, CancellationToken ct = default)
    {
        if (await userRepo.EmailExistsAsync(request.Email, ct))
            return ServiceResult<EmployeeResponse>.Fail("An account with this email already exists.");

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "Employee"
        };
        await userRepo.CreateAsync(user, ct);

        var employee = new Employee
        {
            UserId = user.UserId,
            Department = request.Department,
            Designation = request.Designation,
            Phone = request.Phone
        };
        await employeeRepo.CreateAsync(employee, ct);

        // ── Cache Invalidation ─────────────────────────────────────────────
        // A new employee was added → the cached list is now stale → delete it.
        // Next GET /api/employees will be a cache miss and reload from SQL Server.
        // IMPORTANT: We invalidate AFTER the DB write succeeds.
        // If Redis is down here, we log the error but do NOT fail the request —
        // the employee was already created successfully in SQL Server.
        await redis.DeleteAsync(CacheKeys.AllEmployees);

        var created = await employeeRepo.GetByIdAsync(employee.EmployeeId, ct);
        return ServiceResult<EmployeeResponse>.Ok(MapToResponse(created!));
    }

    // ── UPDATE — Invalidate List + Individual Cache ────────────────────────
    public async Task<EmployeeResponse?> UpdateAsync(int id, UpdateEmployeeRequest request, CancellationToken ct = default)
    {
        var employee = await employeeRepo.GetByIdAsync(id, ct);
        if (employee == null) return null;

        if (request.Department != null) employee.Department = request.Department;
        if (request.Designation != null) employee.Designation = request.Designation;
        if (request.Phone != null) employee.Phone = request.Phone;
        if (request.Name != null) employee.User.Name = request.Name;

        await userRepo.UpdateAsync(employee.User, ct);
        await employeeRepo.UpdateAsync(employee, ct);

        // Invalidate BOTH the list cache AND this employee's individual cache.
        // Both keys are deleted in a single Redis call (more efficient than two calls).
        await redis.DeleteAsync(CacheKeys.AllEmployees, CacheKeys.Employee(id));

        return MapToResponse(employee);
    }

    // ── DELETE — Invalidate List + Individual Cache ────────────────────────
    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        if (!await employeeRepo.ExistsAsync(id, ct)) return false;

        await employeeRepo.DeleteAsync(id, ct);

        // Remove both the list cache and the deleted employee's individual cache
        await redis.DeleteAsync(CacheKeys.AllEmployees, CacheKeys.Employee(id));

        return true;
    }

    // Maps Employee entity to safe DTO — PasswordHash is never included
    private static EmployeeResponse MapToResponse(Employee e) => new()
    {
        EmployeeId = e.EmployeeId,
        UserId = e.UserId,
        Name = e.User.Name,
        Email = e.User.Email,
        Department = e.Department,
        Designation = e.Designation,
        Phone = e.Phone,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}

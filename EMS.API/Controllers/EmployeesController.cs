using System.Security.Claims;
using EMS.API.DTOs;
using EMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize] // All endpoints require a valid JWT by default
public class EmployeesController(
    IEmployeeService employeeService,
    IActivityService activityService) : ControllerBase
{
    // GET /api/employees
    // Both Admin and Employee roles can list all employees
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var employees = await employeeService.GetAllAsync(ct);
        return Ok(employees);
    }

    // GET /api/employees/{id}
    // Records a profile view in Redis after returning the employee data.
    // Fire-and-forget: view recording runs after response — never delays the user.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var employee = await employeeService.GetByIdAsync(id, ct);
        if (employee == null) return NotFound(new { message = "Employee not found." });

        // Record the view AFTER confirming the employee exists.
        // We use _ = Task.Run(...) so this does not block the HTTP response.
        // The viewer's userId comes from JWT claims — never from the request.
        var viewerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        _ = activityService.RecordViewAsync(viewerUserId, id);

        return Ok(employee);
    }

    // GET /api/employees/me — logged-in employee views their own profile
    // IMPORTANT: must be declared before {id:int} to avoid route conflict
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        // Extract UserId from JWT claims — never accept it from the request body
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var employee = await employeeService.GetByUserIdAsync(userId, ct);
        return employee == null ? NotFound(new { message = "Profile not found." }) : Ok(employee);
    }

    // GET /api/employees/search?term=alice
    // Rate limiting will be added to this endpoint in Phase 7
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest(new { message = "Search term is required." });

        var results = await employeeService.SearchAsync(term, ct);
        return Ok(results);
    }

    // POST /api/employees — Admin only
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateEmployeeRequest request, CancellationToken ct)
    {
        var result = await employeeService.CreateAsync(request, ct);

        // ServiceResult.Success = false means a business rule failed (e.g. duplicate email)
        if (!result.Success)
            return Conflict(new { message = result.Error });

        // 201 Created with a Location header pointing to GET /api/employees/{id}
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.EmployeeId }, result.Data);
    }

    // PUT /api/employees/{id}
    // Admin can update any employee; Employee can only update their own profile
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateEmployeeRequest request, CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role);

        // Employees are only allowed to edit their own profile
        if (role != "Admin")
        {
            var myProfile = await employeeService.GetByUserIdAsync(userId, ct);
            if (myProfile == null || myProfile.EmployeeId != id)
                return Forbid(); // 403 Forbidden
        }

        var updated = await employeeService.UpdateAsync(id, request, ct);
        return updated == null ? NotFound(new { message = "Employee not found." }) : Ok(updated);
    }

    // DELETE /api/employees/{id} — Admin only
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await employeeService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound(new { message = "Employee not found." });
    }
}

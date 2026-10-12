namespace EMS.API.DTOs;

// A simple wrapper that lets service methods return either a success value
// or an error message — without throwing exceptions for expected failures
// like "email already exists" or "employee not found".
//
// Usage in controller:
//   var result = await employeeService.CreateAsync(request, ct);
//   if (!result.Success) return BadRequest(new { message = result.Error });
//   return Ok(result.Data);
public class ServiceResult<T>
{
    public bool Success { get; private set; }
    public T? Data { get; private set; }
    public string? Error { get; private set; }

    // Factory methods — cleaner than setting properties manually
    public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static ServiceResult<T> Fail(string error) => new() { Success = false, Error = error };
}

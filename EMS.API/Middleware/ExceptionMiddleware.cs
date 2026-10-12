using System.Net;
using System.Text.Json;

namespace EMS.API.Middleware;

// Catches any unhandled exception from controllers/services and returns
// a clean JSON error response instead of exposing stack traces to clients.
public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context); // Continue to the next middleware/controller
        }
        catch (Exception ex)
        {
            // Log the full exception details on the server side
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            // Return a generic message — never expose internal details to clients
            var response = new { message = "An unexpected error occurred. Please try again later." };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}

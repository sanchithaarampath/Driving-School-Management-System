using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace DSMS.API.Middleware
{
    /// <summary>
    /// Catches every unhandled exception in the pipeline.
    /// Returns a clean JSON response — no stack traces to the client.
    /// All errors are logged with full detail for debugging.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IHostEnvironment env)
        {
            _next   = next;
            _logger = logger;
            _env    = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            // Log the full exception with context
            _logger.LogError(ex,
                "Unhandled exception | {Method} {Path} | User: {User}",
                context.Request.Method,
                context.Request.Path,
                context.User?.Identity?.Name ?? "anonymous");

            context.Response.ContentType = "application/json";

            // Determine HTTP status and user-facing message based on exception type
            var (statusCode, userMessage) = ex switch
            {
                // Entity Framework — DB errors
                DbUpdateException dbEx => (
                    HttpStatusCode.BadRequest,
                    GetDbErrorMessage(dbEx)
                ),

                // Authorization / access
                UnauthorizedAccessException => (
                    HttpStatusCode.Unauthorized,
                    "You are not authorised to perform this action."
                ),

                // Not found (explicit throws)
                KeyNotFoundException => (
                    HttpStatusCode.NotFound,
                    "The requested resource was not found."
                ),

                // Bad argument / validation
                ArgumentException argEx => (
                    HttpStatusCode.BadRequest,
                    argEx.Message
                ),

                // Invalid operations
                InvalidOperationException invEx => (
                    HttpStatusCode.BadRequest,
                    invEx.Message
                ),

                // Everything else — 500
                _ => (
                    HttpStatusCode.InternalServerError,
                    "An unexpected server error occurred. The issue has been logged."
                )
            };

            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                message = userMessage,
                // Only include technical detail in Development — never expose to production
                detail  = _env.IsDevelopment() ? ex.Message : null,
                type    = _env.IsDevelopment() ? ex.GetType().Name : null
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }

        /// <summary>
        /// Converts EF DbUpdateException into a human-readable message.
        /// Avoids leaking raw SQL constraint names to the client.
        /// </summary>
        private static string GetDbErrorMessage(DbUpdateException ex)
        {
            var inner = ex.InnerException?.Message ?? ex.Message;

            if (inner.Contains("FOREIGN KEY") || inner.Contains("FK_"))
                return "This record is linked to other data and cannot be processed. Check related records first.";

            if (inner.Contains("UNIQUE") || inner.Contains("Cannot insert duplicate") || inner.Contains("duplicate key"))
                return "A record with this information already exists. Please use a different value.";

            if (inner.Contains("NULL") || inner.Contains("cannot be null"))
                return "Required information is missing. Please fill in all required fields.";

            if (inner.Contains("String or binary data would be truncated"))
                return "One of the values is too long. Please shorten and try again.";

            return "A database error occurred. Please check your data and try again.";
        }
    }
}

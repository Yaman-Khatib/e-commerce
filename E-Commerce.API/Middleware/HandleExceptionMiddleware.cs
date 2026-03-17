using System.Net;
using System.Text.Json;

namespace E_Commerce_API.Middleware;

public sealed class HandleExceptionMiddleware(RequestDelegate next, ILogger<HandleExceptionMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<HandleExceptionMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing request.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = MapExceptionToResponse(exception);

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            error = message,
            status = context.Response.StatusCode
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }

    private static (HttpStatusCode StatusCode, string Message) MapExceptionToResponse(Exception exception) =>
        exception switch
        {
            ArgumentException argEx => (HttpStatusCode.BadRequest, argEx.Message),
            InvalidOperationException invalidOpEx => (HttpStatusCode.BadRequest, invalidOpEx.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "You are not authorized to perform this action."),
            KeyNotFoundException => (HttpStatusCode.NotFound, "The requested resource was not found."),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
        };
}


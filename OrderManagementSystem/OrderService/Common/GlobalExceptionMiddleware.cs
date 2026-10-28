using System.Net;
using System.Text.Json;

namespace OrderService.Common;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            NotFoundException notFoundEx => (HttpStatusCode.NotFound, notFoundEx.Message),
            ServiceUnavailableException svcEx => (HttpStatusCode.ServiceUnavailable, svcEx.Message),
            AppException appEx => ((HttpStatusCode)appEx.StatusCode, appEx.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized access."),
            _ => (HttpStatusCode.InternalServerError, "An unexpected internal server error occurred.")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse.FailResult(message);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(json);
    }
}

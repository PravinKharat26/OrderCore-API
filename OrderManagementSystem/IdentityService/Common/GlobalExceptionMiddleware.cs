using System.Net;
using System.Text.Json;

namespace IdentityService.Common;

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

        var (statusCode, message, errors) = exception switch
        {
            ValidationException valEx => (HttpStatusCode.BadRequest, valEx.Message, valEx.ValidationErrors),
            NotFoundException notFoundEx => (HttpStatusCode.NotFound, notFoundEx.Message, (List<string>?)null),
            AppException appEx => ((HttpStatusCode)appEx.StatusCode, appEx.Message, (List<string>?)null),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized access.", (List<string>?)null),
            _ => (HttpStatusCode.InternalServerError, "An unexpected internal server error occurred.", (List<string>?)null)
        };

        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse.FailResult(message, errors);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(json);
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InventoryService.ServiceFilters;

// Simple, standard API key filter for inter-service communication
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAuthAttribute : Attribute, IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedApiKey = configuration["InternalApiKey"] ?? "OrderManagementSystem_Internal_ApiKey_Secret_2026!";

        // Check if header exists and matches
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var receivedApiKey) ||
            receivedApiKey != expectedApiKey)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                success = false,
                message = "Unauthorized: Invalid or missing X-Api-Key header."
            });
            return;
        }

        await next();
    }
}

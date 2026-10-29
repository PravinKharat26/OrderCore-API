using Microsoft.AspNetCore.Mvc;

namespace OrderCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    [HttpGet("overview")]
    public IActionResult GetSystemOverview()
    {
        return Ok(new
        {
            System = "OrderCore API - Microservices Architecture",
            Architecture = "Decoupled Microservices with Independent SQLite Databases",
            Services = new object[]
            {
                new { Name = "OrderCore.Gateway", Port = 5080, SwaggerUrl = "http://localhost:5080/swagger", Database = "N/A", Responsibilities = "System Overview, Health Checks, and Gateway Host" },
                new { Name = "IdentityService", Port = 5081, SwaggerUrl = "http://localhost:5081/swagger", Database = "identity.db", Responsibilities = "User Registration, Login, JWT Token Issuance, Role-Based Access" },
                new { Name = "InventoryService", Port = 5082, SwaggerUrl = "http://localhost:5082/swagger", Database = "inventory.db", Responsibilities = "Product Catalog, Stock Levels, Availability Verification" },
                new { Name = "OrderService", Port = 5083, SwaggerUrl = "http://localhost:5083/swagger", Database = "orders.db", Responsibilities = "Order Lifecycle, Authoritative Price Resolution, Inter-Service HTTP Calls" }
            },
            Documentation = "See README.md for architecture specifications and local setup instructions."
        });
    }

    [HttpGet("/health")]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "OrderCore.Gateway",
            Timestamp = DateTime.UtcNow
        });
    }
}

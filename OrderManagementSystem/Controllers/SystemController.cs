using Microsoft.AspNetCore.Mvc;

namespace OrderManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    [HttpGet("overview")]
    public IActionResult GetSystemOverview()
    {
        return Ok(new
        {
            System = "Order Management System - Microservices Architecture",
            Architecture = "Decoupled Microservices with Independent SQLite Databases",
            Services = new object[]
            {
                new { Name = "Gateway / Host", Port = 5080, SwaggerUrl = "http://localhost:5080/swagger", Database = "N/A", Responsibilities = "System Overview, Health Checks, and Gateway Host" },
                new { Name = "IdentityService", Port = 5081, SwaggerUrl = "http://localhost:5081/swagger", Database = "identity.db", Responsibilities = "User Registration, Login, JWT Token Issuance, Role-Based Access" },
                new { Name = "InventoryService", Port = 5082, SwaggerUrl = "http://localhost:5082/swagger", Database = "inventory.db", Responsibilities = "Product Catalog, Stock Levels, Availability Verification" },
                new { Name = "OrderService", Port = 5083, SwaggerUrl = "http://localhost:5083/swagger", Database = "orders.db", Responsibilities = "Order Lifecycle, Authoritative Price Resolution, Inter-Service HTTP Calls" }
            },
            Documentation = "See README.md and docs/interview-guide.md for design patterns and interview preparation."
        });
    }

    [HttpGet("/health")]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "OrderManagementSystem.Gateway",
            Timestamp = DateTime.UtcNow
        });
    }
}

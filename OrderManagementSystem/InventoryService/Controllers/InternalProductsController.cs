using InventoryService.Common;
using InventoryService.DTO;
using InventoryService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InventoryService.Controllers;

[ApiController]
[Route("api/internal/products")]
public class InternalProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public InternalProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Check product stock availability and authoritative price for inter-service order creation
    /// </summary>
    [Authorize]
    [HttpGet("{id:int}/availability")]
    [ProducesResponseType(typeof(ApiResponse<ProductAvailabilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    
    public async Task<IActionResult> CheckAvailability(

        

        int id,
        [FromQuery] int requestedQuantity = 1,
        CancellationToken cancellationToken = default)
    {
        var headerRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (headerRole != "Admin")
        {
            return Forbid();
        }
        var result = await _productService.CheckAvailabilityAsync(id, requestedQuantity, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Internal stock reservation / deduction endpoint called during order processing
    /// </summary>
    [HttpPost("{id:int}/deduct")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeductStock(
        int id,
        [FromQuery] int quantity = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.UpdateStockAsync(id, -quantity, cancellationToken);
        return Ok(result);
    }
}

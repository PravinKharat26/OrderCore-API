using InventoryService.Common;
using InventoryService.DTO;

namespace InventoryService.Interfaces;

public interface IProductService
{
    Task<ApiResponse<PagedResult<ProductDto>>> GetProductsAsync(int pageIndex, int pageSize, string? searchTerm, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductDto>> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductDto>> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductDto>> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductDto>> UpdateStockAsync(int id, int quantityDelta, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductAvailabilityDto>> CheckAvailabilityAsync(int id, int requestedQuantity, CancellationToken cancellationToken = default);
}

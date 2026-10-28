using OrderService.DTO;

namespace OrderService.Interfaces;

public interface IInventoryClient
{
    Task<ProductAvailabilityDto> GetProductAvailabilityAsync(int productId, int quantity, CancellationToken cancellationToken = default);
    Task<bool> DeductStockAsync(int productId, int quantity, CancellationToken cancellationToken = default);
}

using OrderService.Common;
using OrderService.DTO;

namespace OrderService.Interfaces;

public interface IOrderService
{
    Task<ApiResponse<OrderDto>> CreateOrderAsync(Guid customerId, CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<OrderDto>> GetOrderByIdAsync(Guid orderId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<OrderDto>>> GetOrdersAsync(Guid currentUserId, bool isAdmin, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse<OrderDto>> CancelOrderAsync(Guid orderId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
}

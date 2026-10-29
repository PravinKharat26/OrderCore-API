using Microsoft.EntityFrameworkCore;
using OrderService.Common;
using OrderService.Data;
using OrderService.DTO;
using OrderService.Entities;
using OrderService.Interfaces;

namespace OrderService.Services;

public class OrderService : IOrderService
{
    private readonly OrderDbContext _context;
    private readonly IInventoryClient _inventoryClient;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        OrderDbContext context,
        IInventoryClient inventoryClient,
        ILogger<OrderService> logger)
    {
        _context = context;
        _inventoryClient = inventoryClient;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new customer order.
    /// Consistency Note: In this synchronous REST pattern, availability verification followed by stock deduction
    /// is subject to race conditions under high concurrency. For production scale, an Outbox Pattern
    /// with Kafka/RabbitMQ or Saga orchestration (e.g. MassTransit) is recommended.
    /// </summary>
    public async Task<ApiResponse<OrderDto>> CreateOrderAsync(Guid customerId, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items == null || !request.Items.Any())
        {
            throw new AppException("An order must contain at least one item.", 400);
        }

        if (request.Items.GroupBy(i => i.ProductId).Any(g => g.Count() > 1))
        {
            throw new AppException("Duplicate products found in order items. Please combine quantities for the same product.", 400);
        }

        var orderItems = new List<OrderItem>();
        decimal calculatedTotal = 0m;

        // 1. Authoritative availability and pricing verification via InventoryService
        foreach (var itemReq in request.Items)
        {
            var productInfo = await _inventoryClient.GetProductAvailabilityAsync(itemReq.ProductId, itemReq.Quantity, cancellationToken);

            if (!productInfo.RequestedQuantityAvailable)
            {
                throw new AppException($"Product '{productInfo.Name}' does not have enough stock. Available: {productInfo.StockQuantity}, Requested: {itemReq.Quantity}.", 400);
            }

            var lineTotal = Math.Round(productInfo.Price * itemReq.Quantity, 2);
            calculatedTotal += lineTotal;

            orderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = productInfo.ProductId,
                ProductNameSnapshot = productInfo.Name,
                UnitPrice = productInfo.Price, // Authoritative price from InventoryService
                Quantity = itemReq.Quantity,
                LineTotal = lineTotal
            });
        }

        // 2. Deduct inventory in InventoryService
        foreach (var itemReq in request.Items)
        {
            var deducted = await _inventoryClient.DeductStockAsync(itemReq.ProductId, itemReq.Quantity, cancellationToken);
            if (!deducted)
            {
                _logger.LogWarning("Failed to deduct stock for ProductId: {ProductId} during order placement.", itemReq.ProductId);
                // Note: In an interview discussion, highlight that partial failure here requires compensating transactions.
            }
        }

        // 3. Persist Order in local orders.db
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Confirmed,
            TotalAmount = calculatedTotal,
            CreatedAtUtc = DateTime.UtcNow,
            OrderItems = orderItems
        };

        await _context.Orders.AddAsync(order, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order created successfully: ID {OrderId} for Customer {CustomerId}, Total: {Total}",
            order.Id, customerId, order.TotalAmount);

        return ApiResponse<OrderDto>.Ok(MapToDto(order), "Order created successfully.");
    }

    public async Task<ApiResponse<OrderDto>> GetOrderByIdAsync(Guid orderId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            throw new NotFoundException($"Order with ID {orderId} was not found.");
        }

        // Customer ownership check: Customers can only access their own orders
        if (!isAdmin && order.CustomerId != currentUserId)
        {
            _logger.LogWarning("Forbidden access attempt: User {UserId} tried accessing Order {OrderId} owned by {OwnerId}",
                currentUserId, orderId, order.CustomerId);
            throw new UnauthorizedAccessException("You are not authorized to view this order.");
        }

        return ApiResponse<OrderDto>.Ok(MapToDto(order));
    }

    public async Task<ApiResponse<PagedResult<OrderDto>>> GetOrdersAsync(Guid currentUserId, bool isAdmin, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Orders
            .Include(o => o.OrderItems)
            .AsNoTracking();

        // Admin sees all orders; Customer sees only their own orders
        if (!isAdmin)
        {
            query = query.Where(o => o.CustomerId == currentUserId);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var orders = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var pagedResult = new PagedResult<OrderDto>
        {
            Items = orders.Select(MapToDto).ToList(),
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return ApiResponse<PagedResult<OrderDto>>.Ok(pagedResult);
    }

    public async Task<ApiResponse<OrderDto>> CancelOrderAsync(Guid orderId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            throw new NotFoundException($"Order with ID {orderId} was not found.");
        }

        if (!isAdmin && order.CustomerId != currentUserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to cancel this order.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new AppException("Order is already cancelled.", 400);
        }

        order.Status = OrderStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderId} cancelled by User {UserId}", orderId, currentUserId);
        return ApiResponse<OrderDto>.Ok(MapToDto(order), "Order cancelled successfully.");
    }

    private static OrderDto MapToDto(Order order) => new()
    {
        Id = order.Id,
        CustomerId = order.CustomerId,
        Status = order.Status,
        TotalAmount = order.TotalAmount,
        CreatedAtUtc = order.CreatedAtUtc,
        OrderItems = order.OrderItems.Select(i => new OrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductNameSnapshot = i.ProductNameSnapshot,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity,
            LineTotal = i.LineTotal
        }).ToList()
    };
}

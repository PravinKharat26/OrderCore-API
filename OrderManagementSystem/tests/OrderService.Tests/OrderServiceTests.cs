using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Common;
using OrderService.Data;
using OrderService.DTO;
using OrderService.Entities;
using OrderService.Interfaces;
using Xunit;

namespace OrderService.Tests;

public class OrderServiceTests
{
    private readonly OrderDbContext _context;
    private readonly Mock<IInventoryClient> _mockInventoryClient;
    private readonly Mock<ILogger<OrderService.Services.OrderService>> _mockLogger;
    private readonly OrderService.Services.OrderService _orderService;

    public OrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OrderDbContext(options);
        _mockInventoryClient = new Mock<IInventoryClient>();
        _mockLogger = new Mock<ILogger<OrderService.Services.OrderService>>();

        _orderService = new OrderService.Services.OrderService(_context, _mockInventoryClient.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateOrderAsync_ValidItems_CalculatesAuthoritativePriceAndSavesOrder()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        _mockInventoryClient.Setup(c => c.GetProductAvailabilityAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductAvailabilityDto
            {
                ProductId = 1,
                Name = "Mechanical Keyboard",
                Price = 100.00m,
                StockQuantity = 10,
                IsAvailable = true,
                RequestedQuantityAvailable = true
            });

        _mockInventoryClient.Setup(c => c.GetProductAvailabilityAsync(2, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductAvailabilityDto
            {
                ProductId = 2,
                Name = "Mousepad",
                Price = 25.50m,
                StockQuantity = 5,
                IsAvailable = true,
                RequestedQuantityAvailable = true
            });

        _mockInventoryClient.Setup(c => c.DeductStockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = 1, Quantity = 2 }, // 2 * 100 = 200
                new() { ProductId = 2, Quantity = 1 }  // 1 * 25.50 = 25.50
            }
        };

        // Act
        var result = await _orderService.CreateOrderAsync(customerId, request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(225.50m, result.Data.TotalAmount); // Calculated on server
        Assert.Equal(OrderStatus.Confirmed, result.Data.Status);
        Assert.Equal(2, result.Data.OrderItems.Count);

        var orderInDb = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == result.Data.Id);
        Assert.NotNull(orderInDb);
        Assert.Equal(225.50m, orderInDb.TotalAmount);
    }

    [Fact]
    public async Task CreateOrderAsync_InsufficientStock_ThrowsAppException()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        _mockInventoryClient.Setup(c => c.GetProductAvailabilityAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductAvailabilityDto
            {
                ProductId = 1,
                Name = "Ultra Laptop",
                Price = 2000.00m,
                StockQuantity = 2, // only 2 available
                IsAvailable = true,
                RequestedQuantityAvailable = false
            });

        var request = new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = 1, Quantity = 10 }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() => _orderService.CreateOrderAsync(customerId, request));
        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("not have enough stock", ex.Message);
    }

    [Fact]
    public async Task GetOrderByIdAsync_CustomerAccessingOtherCustomerOrder_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var intruderId = Guid.NewGuid();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = ownerId,
            Status = OrderStatus.Confirmed,
            TotalAmount = 50.00m
        };
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        // Act & Assert: intruder is not admin
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _orderService.GetOrderByIdAsync(order.Id, currentUserId: intruderId, isAdmin: false));
    }

    [Fact]
    public async Task GetOrderByIdAsync_AdminAccessingAnyOrder_Succeeds()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Confirmed,
            TotalAmount = 150.00m
        };
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _orderService.GetOrderByIdAsync(order.Id, currentUserId: adminId, isAdmin: true);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(order.Id, result.Data!.Id);
    }

    [Fact]
    public async Task CancelOrderAsync_OwnerCancelsOrder_Succeeds()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Confirmed,
            TotalAmount = 100.00m
        };
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _orderService.CancelOrderAsync(order.Id, currentUserId: customerId, isAdmin: false);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Cancelled, result.Data!.Status);

        var updated = await _context.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Cancelled, updated!.Status);
    }
}

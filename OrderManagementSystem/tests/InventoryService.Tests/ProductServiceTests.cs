using InventoryService.Common;
using InventoryService.Data;
using InventoryService.DTO;
using InventoryService.Entities;
using InventoryService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryService.Tests;

public class ProductServiceTests
{
    private readonly InventoryDbContext _context;
    private readonly Mock<ILogger<ProductService>> _mockLogger;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new InventoryDbContext(options);
        _mockLogger = new Mock<ILogger<ProductService>>();
        _productService = new ProductService(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateProductAsync_ValidRequest_PersistsAndReturnsDto()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Mechanical Keyboard",
            Description = "RGB Backlit keyboard",
            Price = 129.99m,
            StockQuantity = 15
        };

        // Act
        var result = await _productService.CreateProductAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.Id > 0);
        Assert.Equal("Mechanical Keyboard", result.Data.Name);
        Assert.Equal(129.99m, result.Data.Price);
        Assert.Equal(15, result.Data.StockQuantity);

        var productInDb = await _context.Products.FindAsync(result.Data.Id);
        Assert.NotNull(productInDb);
    }

    [Fact]
    public async Task GetProductsAsync_WithSearchTerm_ReturnsFilteredItems()
    {
        // Arrange
        await _context.Products.AddRangeAsync(
            new Product { Name = "Gaming Laptop", Price = 1200, StockQuantity = 5 },
            new Product { Name = "Office Laptop", Price = 800, StockQuantity = 10 },
            new Product { Name = "Wireless Mouse", Price = 50, StockQuantity = 20 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _productService.GetProductsAsync(pageIndex: 1, pageSize: 10, searchTerm: "laptop");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.TotalCount);
        Assert.All(result.Data.Items, p => Assert.Contains("Laptop", p.Name));
    }

    [Fact]
    public async Task GetProductByIdAsync_NonExistingId_ThrowsNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _productService.GetProductByIdAsync(999));
    }

    [Fact]
    public async Task UpdateStockAsync_DeductSufficientStock_UpdatesSuccessfully()
    {
        // Arrange
        var product = new Product { Name = "Monitor", Price = 300, StockQuantity = 10 };
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        // Act: deduct 3
        var result = await _productService.UpdateStockAsync(product.Id, -3);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(7, result.Data!.StockQuantity);

        var updated = await _context.Products.FindAsync(product.Id);
        Assert.Equal(7, updated!.StockQuantity);
    }

    [Fact]
    public async Task UpdateStockAsync_DeductMoreThanAvailable_ThrowsAppException()
    {
        // Arrange
        var product = new Product { Name = "Monitor", Price = 300, StockQuantity = 2 };
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        // Act & Assert: try to deduct 5 when only 2 available
        var ex = await Assert.ThrowsAsync<AppException>(() => _productService.UpdateStockAsync(product.Id, -5));
        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("Insufficient stock", ex.Message);
    }

    [Fact]
    public async Task CheckAvailabilityAsync_ExistingProduct_ReturnsCorrectAvailability()
    {
        // Arrange
        var product = new Product { Name = "Headphones", Price = 199.99m, StockQuantity = 4 };
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        // Act
        var result = await _productService.CheckAvailabilityAsync(product.Id, requestedQuantity: 3);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(199.99m, result.Data!.Price);
        Assert.True(result.Data.IsAvailable);
        Assert.True(result.Data.RequestedQuantityAvailable);
    }
}

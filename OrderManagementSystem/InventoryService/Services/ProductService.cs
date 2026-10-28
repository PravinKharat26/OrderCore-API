using InventoryService.Common;
using InventoryService.Data;
using InventoryService.DTO;
using InventoryService.Entities;
using InventoryService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Services;

public class ProductService : IProductService
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<ProductService> _logger;

    public ProductService(InventoryDbContext context, ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<ProductDto>>> GetProductsAsync(int pageIndex, int pageSize, string? searchTerm, CancellationToken cancellationToken = default)
    {
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search) || p.Description.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(p => MapToDto(p))
            .ToListAsync(cancellationToken);

        var pagedResult = new PagedResult<ProductDto>
        {
            Items = items,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return ApiResponse<PagedResult<ProductDto>>.Ok(pagedResult);
    }

    public async Task<ApiResponse<ProductDto>> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException($"Product with ID {id} was not found.");
        }

        return ApiResponse<ProductDto>.Ok(MapToDto(product));
    }

    public async Task<ApiResponse<ProductDto>> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _context.Products.AddAsync(product, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product created: ID {ProductId}, Name: {Name}", product.Id, product.Name);
        return ApiResponse<ProductDto>.Ok(MapToDto(product), "Product created successfully.");
    }

    public async Task<ApiResponse<ProductDto>> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync(new object[] { id }, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException($"Product with ID {id} was not found.");
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim() ?? string.Empty;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Product updated: ID {ProductId}", product.Id);

        return ApiResponse<ProductDto>.Ok(MapToDto(product), "Product updated successfully.");
    }

    public async Task<ApiResponse<ProductDto>> UpdateStockAsync(int id, int quantityDelta, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync(new object[] { id }, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException($"Product with ID {id} was not found.");
        }

        var newQuantity = product.StockQuantity + quantityDelta;
        if (newQuantity < 0)
        {
            throw new AppException($"Insufficient stock for product '{product.Name}'. Current: {product.StockQuantity}, Requested reduction: {Math.Abs(quantityDelta)}", 400);
        }

        product.StockQuantity = newQuantity;
        product.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Stock updated for Product ID {ProductId}: New Quantity = {NewQuantity}", product.Id, newQuantity);

        return ApiResponse<ProductDto>.Ok(MapToDto(product), "Stock quantity updated successfully.");
    }

    public async Task<ApiResponse<ProductAvailabilityDto>> CheckAvailabilityAsync(int id, int requestedQuantity, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException($"Product with ID {id} was not found.");
        }

        var dto = new ProductAvailabilityDto
        {
            ProductId = product.Id,
            Name = product.Name,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            IsAvailable = product.StockQuantity > 0,
            RequestedQuantityAvailable = product.StockQuantity >= requestedQuantity
        };

        return ApiResponse<ProductAvailabilityDto>.Ok(dto);
    }

    private static ProductDto MapToDto(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        StockQuantity = product.StockQuantity,
        CreatedAtUtc = product.CreatedAtUtc,
        UpdatedAtUtc = product.UpdatedAtUtc
    };
}

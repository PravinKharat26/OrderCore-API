using InventoryService.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Data;

public static class InventoryDataSeeder
{
    public static async Task SeedAsync(InventoryDbContext context, ILogger logger)
    {
        try
        {
            await context.Database.EnsureCreatedAsync();

            if (!await context.Products.AnyAsync())
            {
                var products = new List<Product>
                {
                    new()
                    {
                        Name = "ThinkPad X1 Carbon Gen 11",
                        Description = "Ultralight business laptop with Intel Core i7, 32GB RAM, 1TB SSD.",
                        Price = 1499.99m,
                        StockQuantity = 25,
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "Logitech MX Master 3S",
                        Description = "Performance wireless mouse with quiet clicks and 8K DPI sensor.",
                        Price = 99.99m,
                        StockQuantity = 100,
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "Keychron Q1 Pro Mechanical Keyboard",
                        Description = "Custom wireless mechanical keyboard with hot-swappable switches.",
                        Price = 199.50m,
                        StockQuantity = 50,
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "Dell UltraSharp 27 4K USB-C Hub Monitor",
                        Description = "IPS Black panel with 4K UHD resolution and 90W power delivery.",
                        Price = 549.00m,
                        StockQuantity = 15,
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "Sony WH-1000XM5 Wireless Headphones",
                        Description = "Industry-leading noise canceling headphones with 30-hour battery life.",
                        Price = 398.00m,
                        StockQuantity = 40,
                        CreatedAtUtc = DateTime.UtcNow
                    }
                };

                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} initial products in Inventory database.", products.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed Inventory database.");
            throw;
        }
    }
}

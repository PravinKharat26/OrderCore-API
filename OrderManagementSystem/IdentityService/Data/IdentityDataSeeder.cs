using IdentityService.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Data;

public static class IdentityDataSeeder
{
    public static async Task SeedAsync(IdentityDbContext context, IConfiguration configuration, IPasswordHasher<User> passwordHasher, ILogger logger)
    {
        try
        {
            await context.Database.EnsureCreatedAsync();

            var adminEmail = configuration["AdminUser:Email"] ?? "admin@ordermanagement.local";
            var adminPassword = configuration["AdminUser:Password"] ?? "Admin@12345!";

            if (!await context.Users.AnyAsync(u => u.Role == Roles.Admin))
            {
                var adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    Username = "admin",
                    Email = adminEmail.ToLowerInvariant(),
                    Role = Roles.Admin,
                    CreatedAtUtc = DateTime.UtcNow
                };
                adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, adminPassword);

                await context.Users.AddAsync(adminUser);
                logger.LogInformation("Seeded default Admin user: {Email}", adminEmail);
            }

            var sampleCustomerEmail = "customer@ordermanagement.local";
            if (!await context.Users.AnyAsync(u => u.Email == sampleCustomerEmail))
            {
                var customerUser = new User
                {
                    Id = Guid.NewGuid(),
                    Username = "johndoe",
                    Email = sampleCustomerEmail,
                    Role = Roles.Customer,
                    CreatedAtUtc = DateTime.UtcNow
                };
                customerUser.PasswordHash = passwordHasher.HashPassword(customerUser, "Customer@12345!");

                await context.Users.AddAsync(customerUser);
                logger.LogInformation("Seeded default Customer user: {Email}", sampleCustomerEmail);
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed Identity database.");
            throw;
        }
    }
}

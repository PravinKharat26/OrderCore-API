using IdentityService.Common;
using IdentityService.Data;
using IdentityService.DTO;
using IdentityService.Entities;
using IdentityService.Interfaces;
using IdentityService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IdentityService.Tests;

public class AuthServiceTests
{
    private readonly IdentityDbContext _context;
    private readonly Mock<IPasswordHasher<User>> _mockHasher;
    private readonly Mock<IJwtTokenGenerator> _mockJwtGenerator;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new IdentityDbContext(options);
        _mockHasher = new Mock<IPasswordHasher<User>>();
        _mockJwtGenerator = new Mock<IJwtTokenGenerator>();
        _mockLogger = new Mock<ILogger<AuthService>>();

        _mockHasher.Setup(h => h.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
            .Returns("hashed_password_123");

        _mockJwtGenerator.Setup(j => j.GenerateToken(It.IsAny<User>()))
            .Returns(("mock.jwt.token", DateTime.UtcNow.AddHours(1)));

        _authService = new AuthService(_context, _mockHasher.Object, _mockJwtGenerator.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidCustomerRequest_CreatesCustomerAndReturnsToken()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "alice",
            Email = "alice@example.com",
            Password = "SecurePassword1!"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("mock.jwt.token", result.Data.Token);
        Assert.Equal(Roles.Customer, result.Data.User.Role);
        Assert.Equal("alice@example.com", result.Data.User.Email);

        var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == "alice@example.com");
        Assert.NotNull(userInDb);
        Assert.Equal("hashed_password_123", userInDb.PasswordHash);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsAppException()
    {
        // Arrange
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "bob_original",
            Email = "bob@example.com",
            PasswordHash = "hash",
            Role = Roles.Customer
        };
        await _context.Users.AddAsync(existingUser);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Username = "bob_new",
            Email = "bob@example.com",
            Password = "SecurePassword1!"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() => _authService.RegisterAsync(request));
        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("email", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterAsync_WeakPassword_ThrowsAppException()
    {
        var request = new RegisterRequest
        {
            Username = "weakuser",
            Email = "weak@testdomain.com",
            Password = "weak"
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _authService.RegisterAsync(request));
        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("password", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterAsync_SwaggerPlaceholderValues_ThrowsAppException()
    {
        var request = new RegisterRequest
        {
            Username = "string",
            Email = "user@example.com",
            Password = "string"
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _authService.RegisterAsync(request));
        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("placeholder", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterAsync_WithAdminRole_CreatesAdminUser()
    {
        var request = new RegisterRequest
        {
            Username = "adminuser",
            Email = "adminuser@testdomain.com",
            Password = "SecurePassword1!",
            Role = "Admin"
        };

        var result = await _authService.RegisterAsync(request);
        Assert.True(result.Success);
        Assert.Equal(Roles.Admin, result.Data!.User.Role);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenAndUserDto()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "carol",
            Email = "carol@example.com",
            PasswordHash = "hashed_pw",
            Role = Roles.Customer
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        _mockHasher.Setup(h => h.VerifyHashedPassword(user, "hashed_pw", "CorrectPassword!"))
            .Returns(PasswordVerificationResult.Success);

        var request = new LoginRequest
        {
            Email = "carol@example.com",
            Password = "CorrectPassword!"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("mock.jwt.token", result.Data.Token);
        Assert.Equal("carol", result.Data.User.Username);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsUnauthorizedAppException()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "david",
            Email = "david@example.com",
            PasswordHash = "hashed_pw",
            Role = Roles.Customer
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        _mockHasher.Setup(h => h.VerifyHashedPassword(user, "hashed_pw", "WrongPassword!"))
            .Returns(PasswordVerificationResult.Failed);

        var request = new LoginRequest
        {
            Email = "david@example.com",
            Password = "WrongPassword!"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() => _authService.LoginAsync(request));
        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_NonExistentEmail_ThrowsUnauthorizedAppException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "unknown@example.com",
            Password = "SomePassword!"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() => _authService.LoginAsync(request));
        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ExistingUser_ReturnsUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "eva",
            Email = "eva@example.com",
            PasswordHash = "hash",
            Role = Roles.Admin
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.GetCurrentUserAsync(userId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(userId, result.Data!.Id);
        Assert.Equal(Roles.Admin, result.Data.Role);
    }

    [Fact]
    public async Task GetCurrentUserAsync_NonExistingUser_ThrowsNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _authService.GetCurrentUserAsync(Guid.NewGuid()));
    }
}

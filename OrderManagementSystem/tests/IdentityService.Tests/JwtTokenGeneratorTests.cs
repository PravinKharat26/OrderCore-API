using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using IdentityService.Entities;
using IdentityService.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IdentityService.Tests;

public class JwtTokenGeneratorTests
{
    [Fact]
    public void GenerateToken_ValidUser_ReturnsValidJwtWithExpectedClaims()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Jwt:Key", "SuperSecretTestingKeyForJwtValidation1234567890!"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"},
            {"Jwt:ExpiryMinutes", "30"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var generator = new JwtTokenGenerator(configuration);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            Role = Roles.Admin
        };

        // Act
        var (token, expiresAt) = generator.GenerateToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.True(expiresAt > DateTime.UtcNow);

        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

        Assert.NotNull(jsonToken);
        Assert.Equal("TestIssuer", jsonToken.Issuer);
        Assert.Contains(jsonToken.Audiences, a => a == "TestAudience");

        var roleClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role");
        Assert.NotNull(roleClaim);
        Assert.Equal(Roles.Admin, roleClaim.Value);

        var subClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        Assert.NotNull(subClaim);
        Assert.Equal(user.Id.ToString(), subClaim.Value);
    }
}

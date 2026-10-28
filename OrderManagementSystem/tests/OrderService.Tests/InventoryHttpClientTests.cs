using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using OrderService.Common;
using OrderService.Services;
using Xunit;

namespace OrderService.Tests;

public class InventoryHttpClientTests
{
    private readonly Mock<ILogger<InventoryHttpClient>> _mockLogger;

    public InventoryHttpClientTests()
    {
        _mockLogger = new Mock<ILogger<InventoryHttpClient>>();
    }

    [Fact]
    public async Task GetProductAvailabilityAsync_NetworkFailure_ThrowsServiceUnavailableException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused."));

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:5082/")
        };

        var client = new InventoryHttpClient(httpClient, _mockLogger.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ServiceUnavailableException>(() =>
            client.GetProductAvailabilityAsync(1, 1));

        Assert.Equal(503, ex.StatusCode);
        Assert.Contains("unreachable", ex.Message);
    }

    [Fact]
    public async Task GetProductAvailabilityAsync_ProductNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("{\"message\":\"Not found\"}")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:5082/")
        };

        var client = new InventoryHttpClient(httpClient, _mockLogger.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            client.GetProductAvailabilityAsync(999, 1));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task GetProductAvailabilityAsync_ServiceTimesOut_ThrowsServiceUnavailableException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:5082/")
        };

        var client = new InventoryHttpClient(httpClient, _mockLogger.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ServiceUnavailableException>(() =>
            client.GetProductAvailabilityAsync(1, 1, CancellationToken.None));

        Assert.Equal(503, ex.StatusCode);
        Assert.Contains("timed out", ex.Message);
    }
}

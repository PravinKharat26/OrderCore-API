using System.Net;
using System.Text.Json;
using OrderService.Common;
using OrderService.DTO;
using OrderService.Interfaces;

namespace OrderService.Services;

public class InventoryHttpClient : IInventoryClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryHttpClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public InventoryHttpClient(HttpClient httpClient, ILogger<InventoryHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProductAvailabilityDto> GetProductAvailabilityAsync(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        var endpoint = $"api/internal/products/{productId}/availability?requestedQuantity={quantity}";

        try
        {
            _logger.LogInformation("Calling InventoryService at {BaseAddress}{Endpoint}", _httpClient.BaseAddress, endpoint);
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Product {ProductId} not found in InventoryService.", productId);
                throw new NotFoundException($"Product with ID {productId} does not exist in inventory.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("InventoryService returned error status {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
                throw new AppException($"InventoryService responded with status {response.StatusCode}.", (int)response.StatusCode);
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ProductAvailabilityDto>>(JsonOptions, cancellationToken);
            if (apiResponse?.Data == null)
            {
                throw new AppException("Invalid response payload received from InventoryService.", 502);
            }

            return apiResponse.Data;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to InventoryService at {BaseAddress}", _httpClient.BaseAddress);
            throw new ServiceUnavailableException("InventoryService is currently unreachable. Please retry later.");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "InventoryService call timed out for Product {ProductId}", productId);
            throw new ServiceUnavailableException("InventoryService request timed out.");
        }
    }

    public async Task<bool> DeductStockAsync(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        var endpoint = $"api/internal/products/{productId}/deduct?quantity={quantity}";

        try
        {
            var response = await _httpClient.PostAsync(endpoint, null, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Stock deduction failed for Product {ProductId}: {StatusCode} {Body}", productId, response.StatusCode, errorBody);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stock deduction failed due to network error for Product {ProductId}", productId);
            return false;
        }
    }
}

namespace CoinLore.Clients;

using System.Text.Json;

public abstract class BaseHttpClient
{
    protected readonly HttpClient _httpClient;

    protected readonly ILogger _logger;

    protected BaseHttpClient(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    protected async Task<T> GetAsync<T>(string requestUri)
    {
        try
        {
            _logger.LogInformation($"Sending GET request to {requestUri}");
            var response = await _httpClient.GetAsync(requestUri);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<T>(content, options);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"Request to {requestUri} failed.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while processing the response from {requestUri}.");
            throw;
        }
    }
}
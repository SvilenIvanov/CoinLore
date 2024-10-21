namespace CoinLore.Services;

using Configurations;
using Exceptions;
using Interfaces;
using Microsoft.Extensions.Options;
using Models;

public class CoinMappingService : ICoinMappingService
{
    private readonly ICoinLoreClient _coinLoreClient;
    private readonly ILogger<CoinMappingService> _logger;

    private readonly string _symbolToIdMapFilePath;
    private readonly int _limit;

    public CoinMappingService(ICoinLoreClient coinLoreClient, ILogger<CoinMappingService> logger, IOptions<MappingConfig> mappingConfigOptions)
    {
        _coinLoreClient = coinLoreClient;
        _logger = logger;
        _symbolToIdMapFilePath = mappingConfigOptions.Value.SymbolToIdMapFilePath;
        _limit = mappingConfigOptions.Value.Limit;
    }

    public async Task UpdateCoinMappingAsync()
    {
        try
        {
            var globalData = await FetchGlobalDataAsync();
            var symbolToIdMap = await FetchAndProcessCoinsAsync(globalData.CoinsCount);
            await SaveMappingToFileAsync(symbolToIdMap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the coin mapping.");
            throw;
        }
    }

    private async Task<GlobalData> FetchGlobalDataAsync()
    {
        var globalData = await _coinLoreClient.GetGlobalDataAsync();
        if (globalData == null)
            throw new HttpStatusCodeException(400, "Failed to retrieve global data.");

        return globalData;
    }

    private async Task<Dictionary<string, long>> FetchAndProcessCoinsAsync(int coinsCount)
    {
        if (coinsCount <= 0)
        {
            _logger.LogWarning("Coins count retrieved is zero or negative.");
            return new Dictionary<string, long>();
        }

        int numberOfCalls = (int)Math.Ceiling((double)coinsCount / _limit);
        _logger.LogInformation($"Number of API calls required: {numberOfCalls}");

        var tasks = Enumerable.Range(0, numberOfCalls)
            .Select(i => FetchCoinsAsync(i * _limit, _limit))
            .ToList();

        var results = await Task.WhenAll(tasks);

        var symbolToIdMap = results
            .Where(coins => coins != null && coins.Count > 0)
            .SelectMany(coins => coins)
            .Where(coin => !string.IsNullOrWhiteSpace(coin.Symbol) && !string.IsNullOrWhiteSpace(coin.Id))
            .GroupBy(coin => coin.Symbol.ToUpper())
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    if (long.TryParse(g.First().Id, out var id))
                        return id;
                    else
                    {
                        _logger.LogWarning($"Invalid ID format for symbol {g.Key}: {g.First().Id}");
                        return 0L;
                    }
                });

        return symbolToIdMap;
    }

    private async Task<List<CoinTicker>> FetchCoinsAsync(int start, int limit)
    {
        var coins = await _coinLoreClient.GetTickersByPaginationAsync(start, limit);
        if (coins == null || coins.Count == 0)
        {
            _logger.LogWarning($"No coins retrieved for start={start}");
            return new List<CoinTicker>();
        }

        _logger.LogInformation($"Fetched coins from {start} to {start + limit - 1}");
        return coins;
    }

    private async Task SaveMappingToFileAsync(Dictionary<string, long> symbolToIdMap)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(symbolToIdMap, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(_symbolToIdMapFilePath, json);
        _logger.LogInformation("Symbol to ID mapping saved successfully.");
    }
}

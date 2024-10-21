namespace CoinLore.Services;

using CoinLore.Models.CoinLore;
using Interfaces;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

public class CoinPriceService : ICoinPriceService
{
    private readonly ICoinLoreClient _coinLoreClient;
    private readonly ISymbolToIdMappingService _mappingService;
    private readonly ILogger<CoinPriceService> _logger;

    public CoinPriceService(
        ICoinLoreClient coinLoreClient,
        ISymbolToIdMappingService mappingService,
        ILogger<CoinPriceService> logger)
    {
        _coinLoreClient = coinLoreClient;
        _mappingService = mappingService;
        _logger = logger;
    }

    public async Task<Dictionary<string, decimal>> GetCurrentPricesAsync(IEnumerable<string> coinSymbols)
    {
        try
        {
            var symbolToIdMap = await _mappingService.GetSymbolToIdMapAsync();

            if (symbolToIdMap == null || symbolToIdMap.Count == 0)
            {
                _logger.LogWarning("Symbol to ID mapping is empty or null.");
                return [];
            }

            var ids = coinSymbols
                .Select(s => symbolToIdMap.TryGetValue(s.ToUpperInvariant(), out var id) ? id.ToString() : null)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList();

            if (ids.Count == 0)
            {
                _logger.LogWarning("No valid coin IDs found for the given symbols.");
                return [];
            }

            var coinTickers = await _coinLoreClient.GetTickersByIdsAsync(ids);

            if (coinTickers == null)
            {
                _logger.LogWarning("No coin tickers retrieved from the API.");
                return [];
            }

            var prices = new Dictionary<string, decimal>();

            ProcessTickers(coinTickers, prices);

            return prices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching current prices.");
            throw;
        }
    }

    private void ProcessTickers(List<CoinTicker> coinTickers, Dictionary<string, decimal> prices)
    {
        foreach (var ticker in coinTickers)
        {
            if (ticker == null)
            {
                _logger.LogWarning("Encountered a null ticker.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(ticker.Symbol) || string.IsNullOrWhiteSpace(ticker.PriceUsd))
            {
                _logger.LogWarning("Ticker has invalid Symbol or PriceUsd.");
                continue;
            }

            if (decimal.TryParse(ticker.PriceUsd, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
            {
                prices[ticker.Symbol.ToUpperInvariant()] = price;
            }
            else
            {
                _logger.LogWarning("Unable to parse price for symbol {Symbol}: {PriceUsd}", ticker.Symbol, ticker.PriceUsd);
                prices[ticker.Symbol.ToUpperInvariant()] = 0;
            }
        }
    }
}
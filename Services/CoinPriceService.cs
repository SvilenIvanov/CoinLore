namespace CoinLore.Services;

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
        var symbolToIdMap = await _mappingService.GetSymbolToIdMapAsync();

        var ids = coinSymbols
            .Select(s => symbolToIdMap.TryGetValue(s.ToUpperInvariant(), out var id) ? id.ToString() : null)
            .Where(id => id != null)
            .ToList();

        if (ids.Count == 0)
        {
            _logger.LogWarning("No valid coin IDs found for the given symbols.");
            return [];
        }

        var coinTickers = await _coinLoreClient.GetTickersByIdsAsync(ids);

        var prices = new Dictionary<string, decimal>();

        foreach (var ticker in coinTickers)
        {
            if (decimal.TryParse(ticker.PriceUsd, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
            {
                prices[ticker.Symbol.ToUpperInvariant()] = price;
            }
            else
            {
                _logger.LogWarning("Unable to parse price for symbol {Symbol}", ticker.Symbol);
                prices[ticker.Symbol.ToUpperInvariant()] = 0;
            }
        }

        return prices;
    }
}
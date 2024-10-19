namespace CoinLore.Services;

using Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CoinPriceService : ICoinPriceService
{
    private readonly ICoinLoreClient _coinLoreClient;
    private readonly ILogger<CoinPriceService> _logger;
    private readonly IMemoryCache _cache;

    public CoinPriceService(
        ICoinLoreClient coinLoreClient,
        ILogger<CoinPriceService> logger,
        IMemoryCache cache)
    {
        _coinLoreClient = coinLoreClient;
        _logger = logger;
        _cache = cache;
    }

    public async Task<Dictionary<string, decimal>> GetCurrentPricesAsync(IEnumerable<string> coinSymbols)
    {
        var symbolSet = new HashSet<string>(coinSymbols.Select(s => s.ToUpper()));
        var prices = new Dictionary<string, decimal>();
        var symbolsToFetch = new List<string>();

        foreach (var symbol in symbolSet)
        {
            if (_cache.TryGetValue($"Price_{symbol}", out decimal cachedPrice))
            {
                prices[symbol] = cachedPrice;
            }
            else
            {
                symbolsToFetch.Add(symbol);
            }
        }

        if (symbolsToFetch.Any())
        {
            var fetchedPrices = await FetchPricesAsync(symbolsToFetch);

            foreach (var kvp in fetchedPrices)
            {
                prices[kvp.Key] = kvp.Value;
                _cache.Set($"Price_{kvp.Key}", kvp.Value, TimeSpan.FromMinutes(5));
            }
        }

        return prices;
    }

    private async Task<Dictionary<string, decimal>> FetchPricesAsync(IEnumerable<string> symbols)
    {
        var symbolSet = new HashSet<string>(symbols.Select(s => s.ToUpper()));
        var prices = new Dictionary<string, decimal>();
        var symbolToId = new Dictionary<string, string>();

        var topTickers = await _coinLoreClient.GetTopTickersAsync();
        var topTickersDict = topTickers.ToDictionary(t => t.Symbol.ToUpper(), t => t);

        foreach (var symbol in symbolSet)
        {
            if (topTickersDict.TryGetValue(symbol, out var ticker))
            {
                prices[symbol] = ticker.PriceUsd;
                symbolToId[symbol] = ticker.Id;
            }
        }

        var symbolsNotFound = symbolSet.Except(prices.Keys);

        if (symbolsNotFound.Any())
        {
            foreach (var symbol in symbolsNotFound)
            {
                if (_cache.TryGetValue($"Id_{symbol}", out string id))
                {
                    symbolToId[symbol] = id;
                }
                else
                {
                    await UpdateSymbolToIdCacheAsync(symbol);
                    if (_cache.TryGetValue($"Id_{symbol}", out id))
                    {
                        symbolToId[symbol] = id;
                    }
                }
            }

            var idsToFetch = symbolToId.Values.Distinct();

            if (idsToFetch.Any())
            {
                var missingTickers = await _coinLoreClient.GetTickersByIdsAsync(idsToFetch);

                foreach (var ticker in missingTickers)
                {
                    var symbol = ticker.Symbol.ToUpper();
                    prices[symbol] = ticker.PriceUsd;

                    _cache.Set($"Price_{symbol}", ticker.PriceUsd, TimeSpan.FromMinutes(5));
                }
            }
        }

        return prices;
    }

    private async Task UpdateSymbolToIdCacheAsync(string symbol)
    {
        int start = 0;
        int limit = 100;
        bool found = false;

        while (!found)
        {
            var coins = await _coinLoreClient.GetTickersByPaginationAsync(start, limit);

            if (coins == null || !coins.Any())
            {
                break;
            }

            foreach (var coin in coins)
            {
                var coinSymbol = coin.Symbol.ToUpper();

                if (!_cache.TryGetValue($"Id_{coinSymbol}", out _))
                {
                    _cache.Set($"Id_{coinSymbol}", coin.Id, TimeSpan.FromHours(1));
                }

                if (coinSymbol == symbol)
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                break;
            }

            start += limit;
        }
    }
}
namespace CoinLore.Services;

using CoinLore.Interfaces;
using Models;
using System.Collections.Concurrent;

public class InMemoryPortfolioRepository : IPortfolioRepository
{
    private readonly ConcurrentDictionary<string, PortfolioItem> _portfolioItems = new();
    private readonly ConcurrentDictionary<string, decimal> _currentPrices = new();

    public async Task UploadPortfolioAsync(List<PortfolioItem> items)
    {
        _portfolioItems.Clear();
        foreach (var item in items)
        {
            _portfolioItems[item.Coin] = item;
        }
        await Task.CompletedTask;
    }

    public async Task<List<PortfolioItem>> GetPortfolioItemsAsync()
    {
        return await Task.FromResult(_portfolioItems.Values.ToList());
    }

    public void UpdateCurrentPrice(string coin, decimal currentPrice)
    {
        _currentPrices[coin] = currentPrice;
    }

    public decimal GetCurrentPrice(string coin)
    {
        return _currentPrices.TryGetValue(coin, out var price) ? price : 0;
    }

    public List<string> GetAllSymbols()
    {
        return _portfolioItems.Keys.ToList();
    }
}
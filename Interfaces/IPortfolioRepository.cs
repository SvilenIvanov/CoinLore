using CoinLore.Models;

namespace CoinLore.Interfaces;
public interface IPortfolioRepository
{
    List<string> GetAllSymbols();
    decimal GetCurrentPrice(string coin);
    Task<List<PortfolioItem>> GetPortfolioItemsAsync();
    void UpdateCurrentPrice(string coin, decimal currentPrice);
    Task UploadPortfolioAsync(List<PortfolioItem> items);
}
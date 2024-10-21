namespace CoinLore.Interfaces;

using Models;

public interface IPortfolioRepository
{
    List<string> GetAllSymbols();
    decimal GetCurrentPrice(string coin);
    Task<List<PortfolioItem>> GetPortfolioItemsAsync();
    void UpdateCurrentPrice(string coin, decimal currentPrice);
    Task UploadPortfolioAsync(List<PortfolioItem> items);
}
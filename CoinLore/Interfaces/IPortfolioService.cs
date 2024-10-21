namespace CoinLore.Interfaces;

using Models;

public interface IPortfolioService
{
    Task UploadPortfolioAsync(IFormFile file);

    Task<PortfolioSummary> GetPortfolioSummaryAsync();
}

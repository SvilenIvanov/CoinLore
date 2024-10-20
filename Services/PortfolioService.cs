namespace CoinLore.Services;

using CoinLore.Models;
using Interfaces;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public class PortfolioService : IPortfolioService
{
    public Task<PortfolioSummary> GetPortfolioSummaryAsync()
    {
        throw new NotImplementedException();
    }

    public Task UploadPortfolioAsync(IFormFile file)
    {
        throw new NotImplementedException();
    }
}

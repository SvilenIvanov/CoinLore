namespace CoinLore.Services;

using Exceptions;
using Interfaces;
using Microsoft.AspNetCore.Http;
using Models;
using System.Globalization;
using System.Threading.Tasks;

public class PortfolioService : IPortfolioService
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly ISymbolToIdMappingService _mappingService;
    private readonly ILogger<PortfolioService> _logger;

    public PortfolioService(
        IPortfolioRepository portfolioRepository,
        ILogger<PortfolioService> logger,
        ISymbolToIdMappingService mappingService)
    {
        _portfolioRepository = portfolioRepository;
        _logger = logger;
        _mappingService = mappingService;
    }

    public async Task UploadPortfolioAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new HttpStatusCodeException(400, "Invalid file.");

        var symbolToIdMap = await _mappingService.GetSymbolToIdMapAsync();

        if (symbolToIdMap == null || symbolToIdMap.Count == 0)
            throw new HttpStatusCodeException(400, "No loaded symbols are found!");

        var items = new List<PortfolioItem>();

        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);

        string line;
        var lineNumber = 0;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split('|');
            if (parts.Length != 3)
            {
                _logger.LogWarning($"Invalid line format at line {lineNumber}: {line}");
                continue;
            }

            if (!decimal.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity))
            {
                _logger.LogWarning($"Invalid quantity at line {lineNumber}: {line}");
                continue;
            }

            var coin = parts[1].Trim().ToUpperInvariant();

            if (!decimal.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var initialPrice))
            {
                _logger.LogWarning($"Invalid initial price at line {lineNumber}: {parts[2]}");
                continue;
            }

            if (!symbolToIdMap.TryGetValue(coin, out var id))
            {
                _logger.LogWarning("Coin symbol {Coin} not found in mapping at line {LineNumber}. Skipping.", coin, lineNumber);
                continue;
            }

            items.Add(new PortfolioItem
            {
                Id = id,
                Quantity = quantity,
                Coin = coin,
                InitialPrice = initialPrice
            });
        }

        await _portfolioRepository.UploadPortfolioAsync(items);
        _logger.LogInformation("Portfolio uploaded and parsed successfully.");
    }

    public async Task<PortfolioSummary> GetPortfolioSummaryAsync()
    {
        var items = await _portfolioRepository.GetPortfolioItemsAsync();

        if (items == null || items.Count == 0)
            throw new HttpStatusCodeException(400, "Portfolio is empty. Please upload a portfolio file.");

        var coinChanges = new List<CoinChange>();
        var initialValue = 0m;
        var currentValue = 0m;

        foreach (var item in items)
        {
            var currentPrice = _portfolioRepository.GetCurrentPrice(item.Coin);

            var coinChange = new CoinChange
            {
                Coin = item.Coin,
                Quantity = item.Quantity,
                InitialPrice = item.InitialPrice,
                CurrentPrice = currentPrice
            };

            coinChanges.Add(coinChange);

            initialValue += coinChange.InitialValue;
            currentValue += coinChange.CurrentValue;
        }

        var overallChangePercentage = initialValue == 0 ? 0 : ((currentValue - initialValue) / initialValue) * 100;

        var summary = new PortfolioSummary
        {
            InitialValue = initialValue,
            CurrentValue = currentValue,
            OverallChangePercentage = overallChangePercentage,
            CoinChanges = coinChanges
        };

        return summary;
    }
}
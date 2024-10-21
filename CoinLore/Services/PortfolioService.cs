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
    private readonly IPriceUpdateService _priceUpdateService;
    private readonly ILogger<PortfolioService> _logger;

    public PortfolioService(
        IPortfolioRepository portfolioRepository,
        ILogger<PortfolioService> logger,
        ISymbolToIdMappingService mappingService,
        IPriceUpdateService priceUpdateService)
    {
        _portfolioRepository = portfolioRepository;
        _logger = logger;
        _mappingService = mappingService;
        _priceUpdateService = priceUpdateService;
    }

    public async Task UploadPortfolioAsync(IFormFile file)
    {
        ValidateFile(file);

        var symbolToIdMap = await RetrieveSymbolToIdMapAsync();
        var items = await ParseAndValidatePortfolioAsync(file.OpenReadStream(), symbolToIdMap);

        await UploadAndUpdatePricesAsync(items);
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

        return new PortfolioSummary
        {
            InitialValue = initialValue,
            CurrentValue = currentValue,
            OverallChangePercentage = overallChangePercentage,
            CoinChanges = coinChanges
        };
    }

    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Invalid file.");
    }

    private async Task<Dictionary<string, long>> RetrieveSymbolToIdMapAsync()
    {
        var symbolToIdMap = await _mappingService.GetSymbolToIdMapAsync();
        if (symbolToIdMap == null || symbolToIdMap.Count == 0)
            throw new InvalidOperationException("No loaded symbols are found!");

        return symbolToIdMap;
    }

    private async Task<List<PortfolioItem>> ParseAndValidatePortfolioAsync(Stream fileStream, Dictionary<string, long> symbolToIdMap)
    {
        var items = await ParsePortfolioFileAsync(fileStream, symbolToIdMap);
        if (items.Count == 0)
        {
            _logger.LogWarning("No valid portfolio items were parsed from the uploaded file.");
            throw new InvalidOperationException("No valid portfolio items found in the uploaded file.");
        }
        return items;
    }

    private async Task UploadAndUpdatePricesAsync(List<PortfolioItem> items)
    {
        await _portfolioRepository.UploadPortfolioAsync(items);
        _logger.LogInformation("Portfolio uploaded and parsed successfully.");

        await _priceUpdateService.UpdatePricesAsync();
        _logger.LogInformation("Prices updated immediately after portfolio upload.");
    }

    private async Task<List<PortfolioItem>> ParsePortfolioFileAsync(Stream fileStream, Dictionary<string, long> symbolToIdMap)
    {
        var items = new List<PortfolioItem>();
        using var reader = new StreamReader(fileStream);
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
                _logger.LogWarning($"Invalid quantity at line {lineNumber}: {parts[0]}");
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

            if (quantity <= 0)
            {
                _logger.LogWarning($"Non-positive quantity at line {lineNumber}: {quantity}");
                continue;
            }

            if (initialPrice < 0)
            {
                _logger.LogWarning($"Negative initial price at line {lineNumber}: {initialPrice}");
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

        return items;
    }
}
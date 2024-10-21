namespace CoinLore.Services;

using Configurations;
using Interfaces;
using Microsoft.Extensions.Options;

public class PriceUpdateBackgroundService : BackgroundService
{
    private readonly ICoinPriceService _coinPriceService;
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly ILogger<PriceUpdateBackgroundService> _logger;
    private readonly TimeSpan _updateInterval;

    public PriceUpdateBackgroundService(
        ICoinPriceService coinPriceService,
        IPortfolioRepository portfolioRepository,
        IOptions<PortfolioConfig> portfolioConfig,
        ILogger<PriceUpdateBackgroundService> logger)
    {
        _coinPriceService = coinPriceService;
        _portfolioRepository = portfolioRepository;
        _logger = logger;

        var config = portfolioConfig.Value;
        _updateInterval = TimeSpan.FromMinutes(config.PriceUpdateIntervalInMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PriceUpdateBackgroundService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdatePricesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating prices.");
            }

            await Task.Delay(_updateInterval, stoppingToken);
        }

        _logger.LogInformation("PriceUpdateBackgroundService is stopping.");
    }

    private async Task UpdatePricesAsync()
    {
        var symbols = _portfolioRepository.GetAllSymbols();

        if (symbols.Count == 0)
        {
            _logger.LogInformation("No symbols to update.");
            return;
        }

        var prices = await _coinPriceService.GetCurrentPricesAsync(symbols);

        foreach (var symbol in symbols)
        {
            if (prices.TryGetValue(symbol, out var price))
            {
                _portfolioRepository.UpdateCurrentPrice(symbol, price);
            }
            else
            {
                _logger.LogWarning("Price not found for symbol {Symbol}", symbol);
                _portfolioRepository.UpdateCurrentPrice(symbol, 0);
            }
        }

        _logger.LogInformation("Prices updated at {Time}", DateTime.Now);
    }
}

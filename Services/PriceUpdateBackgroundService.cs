namespace CoinLore.Services;

using Configurations;
using Interfaces;
using Microsoft.Extensions.Options;

public class PriceUpdateBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PriceUpdateBackgroundService> _logger;
    private readonly TimeSpan _updateInterval;

    public PriceUpdateBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<PortfolioConfig> options,
        ILogger<PriceUpdateBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var config = options.Value;
        _updateInterval = TimeSpan.FromSeconds(20); // TimeSpan.FromMinutes(config.PriceUpdateIntervalInMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PriceUpdateBackgroundService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();

                var coinPriceService = scope.ServiceProvider.GetRequiredService<ICoinPriceService>();
                var portfolioRepository = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();

                await UpdatePricesAsync(coinPriceService, portfolioRepository);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating prices.");
            }

            await Task.Delay(_updateInterval, stoppingToken);
        }

        _logger.LogInformation("PriceUpdateBackgroundService is stopping.");
    }

    private async Task UpdatePricesAsync(
        ICoinPriceService coinPriceService,
        IPortfolioRepository portfolioRepository)
    {
        var symbols = portfolioRepository.GetAllSymbols();

        if (symbols.Count == 0)
        {
            _logger.LogInformation("No symbols to update.");
            return;
        }

        var prices = await coinPriceService.GetCurrentPricesAsync(symbols);

        foreach (var symbol in symbols)
        {
            if (prices.TryGetValue(symbol, out var price))
            {
                portfolioRepository.UpdateCurrentPrice(symbol, price);
            }
            else
            {
                _logger.LogWarning("Price not found for symbol {Symbol}", symbol);
                portfolioRepository.UpdateCurrentPrice(symbol, 0);
            }
        }

        _logger.LogInformation("Prices updated at {Time}", DateTime.Now);
    }
}
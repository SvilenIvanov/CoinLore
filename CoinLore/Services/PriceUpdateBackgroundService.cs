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
        _updateInterval = TimeSpan.FromMinutes(options.Value.PriceUpdateIntervalInMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PriceUpdateBackgroundService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();

                var priceUpdateService = scope.ServiceProvider.GetRequiredService<IPriceUpdateService>();
                await priceUpdateService.UpdatePricesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating prices.");
            }

            await Task.Delay(_updateInterval, stoppingToken);
        }

        _logger.LogInformation("PriceUpdateBackgroundService is stopping.");
    }
}
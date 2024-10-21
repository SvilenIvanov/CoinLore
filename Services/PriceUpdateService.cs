namespace CoinLore.Services;

using Interfaces;

public class PriceUpdateService : IPriceUpdateService
{
    private readonly ICoinPriceService _coinPriceService;
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly ILogger<PriceUpdateService> _logger;

    public PriceUpdateService(
        ICoinPriceService coinPriceService,
        IPortfolioRepository portfolioRepository,
        ILogger<PriceUpdateService> logger)
    {
        _coinPriceService = coinPriceService;
        _portfolioRepository = portfolioRepository;
        _logger = logger;
    }

    public async Task UpdatePricesAsync()
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
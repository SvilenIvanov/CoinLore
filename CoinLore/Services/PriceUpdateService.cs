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
        try
        {
            var symbols = _portfolioRepository.GetAllSymbols();

            if (symbols == null || symbols.Count == 0)
            {
                _logger.LogInformation("No symbols to update.");
                return;
            }

            var prices = await _coinPriceService.GetCurrentPricesAsync(symbols);

            if (prices == null || prices.Count == 0)
            {
                _logger.LogWarning("No prices retrieved from the CoinPriceService.");
                return;
            }

            foreach (var symbol in symbols)
            {
                if (prices.TryGetValue(symbol, out var price))
                {
                    if (price < 0)
                    {
                        _logger.LogWarning("Received negative price for symbol {Symbol}: {Price}", symbol, price);
                        continue;
                    }

                    _portfolioRepository.UpdateCurrentPrice(symbol, price);
                    _logger.LogInformation("Updated price for {Symbol}: {Price}", symbol, price);
                }
                else
                {
                    _logger.LogWarning("Price not found for symbol {Symbol}. Setting price to 0.", symbol);
                    _portfolioRepository.UpdateCurrentPrice(symbol, 0);
                }
            }

            _logger.LogInformation("Prices updated at {Time}", DateTime.Now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating prices.");
            throw;
        }
    }

}
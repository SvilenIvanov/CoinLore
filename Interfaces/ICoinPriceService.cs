namespace CoinLore.Interfaces;

public interface ICoinPriceService
{
    Task<Dictionary<string, decimal>> GetCurrentPricesAsync(IEnumerable<string> coinSymbols);
}
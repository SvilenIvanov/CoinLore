namespace CoinLore.Services;

using Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CoinPriceService : ICoinPriceService
{
    public Task<Dictionary<string, decimal>> GetCurrentPricesAsync(IEnumerable<string> coinSymbols)
    {
        throw new NotImplementedException();
    }
}

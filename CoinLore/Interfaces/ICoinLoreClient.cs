﻿namespace CoinLore.Interfaces;

using Models.CoinLore;

public interface ICoinLoreClient
{
    Task<List<CoinTicker>> GetTickersByIdsAsync(IEnumerable<string> ids);

    Task<List<CoinTicker>> GetTickersByPaginationAsync(int start, int limit);

    Task<GlobalData?> GetGlobalDataAsync();
}
namespace CoinLore.Clients;

using Configurations;
using Interfaces;
using Microsoft.Extensions.Options;
using Models.CoinLore;

public class CoinLoreClient : BaseHttpClient, ICoinLoreClient
{
    private readonly CoinLoreConfig _config;

    public CoinLoreClient(
        HttpClient httpClient,
        ILogger<CoinLoreClient> logger,
        IOptionsMonitor<CoinLoreConfig> optionsMonitor)
        : base(httpClient, logger)
    {
        _config = optionsMonitor.CurrentValue;
    }

    public async Task<GlobalData?> GetGlobalDataAsync()
    {
        var endpoint = _config.Endpoints.Global;
        var response = await GetAsync<List<GlobalData>>(endpoint);
        return response.Count > 0 ? response[0] : null;
    }

    public async Task<List<CoinTicker>> GetTickersByIdsAsync(IEnumerable<string> ids)
    {
        var idString = string.Join(",", ids);
        var endpointTemplate = _config.Endpoints.TickerById;
        var endpoint = string.Format(endpointTemplate, idString);
        var tickers = await GetAsync<List<CoinTicker>>(endpoint);

        return tickers;
    }

    public async Task<List<CoinTicker>> GetTickersByPaginationAsync(int start, int limit)
    {
        var endpointTemplate = _config.Endpoints.TickersByPagination;
        var endpoint = string.Format(endpointTemplate, start, limit);
        var response = await GetAsync<CoinTickerResponse>(endpoint);

        return response.Data;
    }
}
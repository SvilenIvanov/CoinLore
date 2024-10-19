namespace CoinLore.Configurations;

public class CoinLoreConfig
{
    public string BaseUrl { get; set; }

    public CoinLoreEndpoints Endpoints { get; set; }
}

public class CoinLoreEndpoints
{
    public string Tickers { get; set; }

    public string TickerById { get; set; }

    public string TickersByPagination { get; set; }
}
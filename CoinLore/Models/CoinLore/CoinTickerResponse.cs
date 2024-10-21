namespace CoinLore.Models.CoinLore;

using System.Text.Json.Serialization;

public class CoinTickerResponse
{
    [JsonPropertyName("data")]
    public List<CoinTicker> Data { get; set; }
}
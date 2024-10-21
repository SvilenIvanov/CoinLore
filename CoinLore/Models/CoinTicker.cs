namespace CoinLore.Models;

using System.Text.Json.Serialization;

public class CoinTicker
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; }

    [JsonPropertyName("price_usd")]
    public string PriceUsd { get; set; }
}
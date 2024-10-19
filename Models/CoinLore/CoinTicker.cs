namespace CoinLore.Models.CoinLore;

using System.Text.Json.Serialization;

public class CoinTicker
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; }

    [JsonPropertyName("price_usd")]
    public decimal PriceUsd { get; set; }
}
namespace CoinLore.Models.CoinLore;

using System.Text.Json.Serialization;

public class GlobalData
{
    [JsonPropertyName("coins_count")]
    public int CoinsCount { get; set; }
}
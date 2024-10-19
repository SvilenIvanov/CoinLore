namespace CoinLore.Models;

public class CoinChange
{
    public string Coin { get; set; }
    public decimal InitialValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal ChangePercentage { get; set; }
}
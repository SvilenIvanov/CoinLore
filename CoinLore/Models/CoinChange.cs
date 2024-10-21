namespace CoinLore.Models;

public class CoinChange
{
    public string Coin { get; set; }

    public decimal Quantity { get; set; }

    public decimal InitialPrice { get; set; }

    public decimal CurrentPrice { get; set; }

    public decimal InitialValue => Quantity * InitialPrice;

    public decimal CurrentValue => Quantity * CurrentPrice;

    public decimal ChangePercentage => InitialPrice == 0 ? 0 : ((CurrentPrice - InitialPrice) / InitialPrice) * 100;
}
namespace CoinLore.Models;

public class PortfolioSummary
{
    public decimal InitialValue { get; set; }

    public decimal CurrentValue { get; set; }

    public decimal OverallChangePercentage { get; set; }

    public List<CoinChange> CoinChanges { get; set; }
}
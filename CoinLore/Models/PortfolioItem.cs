﻿namespace CoinLore.Models;

public class PortfolioItem
{
    public long Id { get; set; }

    public decimal Quantity { get; set; }

    public string Coin { get; set; }

    public decimal InitialPrice { get; set; }
}
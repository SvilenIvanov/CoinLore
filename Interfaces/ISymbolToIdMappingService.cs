namespace CoinLore.Interfaces;

public interface ISymbolToIdMappingService
{
    Task<Dictionary<string, long>> GetSymbolToIdMapAsync();
}
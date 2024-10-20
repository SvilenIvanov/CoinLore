namespace CoinLore.Services;

using Configurations;
using Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;

public class SymbolToIdMappingService : ISymbolToIdMappingService
{
    private readonly string _symbolToIdMapFilePath;
    private readonly ILogger<SymbolToIdMappingService> _logger;
    private readonly Lazy<Task<Dictionary<string, long>>> _lazyMap;

    public SymbolToIdMappingService(
        IOptions<MappingConfig> mappingConfigOptions,
        ILogger<SymbolToIdMappingService> logger)
    {
        _symbolToIdMapFilePath = mappingConfigOptions.Value.SymbolToIdMapFilePath;
        _logger = logger;

        _lazyMap = new Lazy<Task<Dictionary<string, long>>>(LoadMappingAsync, true);
    }

    private async Task<Dictionary<string, long>> LoadMappingAsync()
    {
        if (File.Exists(_symbolToIdMapFilePath))
        {
            var json = await File.ReadAllTextAsync(_symbolToIdMapFilePath);
            var mapping = JsonSerializer.Deserialize<Dictionary<string, long>>(json);
            _logger.LogInformation("Symbol to ID mapping loaded successfully.");
            return mapping;
        }
        else
        {
            _logger.LogError("Symbol to ID mapping file not found at {FilePath}.", _symbolToIdMapFilePath);
            throw new FileNotFoundException("Symbol to ID mapping file not found.", _symbolToIdMapFilePath);
        }
    }

    public Task<Dictionary<string, long>> GetSymbolToIdMapAsync()
    {
        return _lazyMap.Value;
    }
}
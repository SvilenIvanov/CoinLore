namespace CoinLore.Services;

using Configurations;
using Interfaces;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Text.Json;

public class SymbolToIdMappingService : ISymbolToIdMappingService
{
    private readonly string _symbolToIdMapFilePath;
    private readonly ILogger<SymbolToIdMappingService> _logger;
    private Dictionary<string, long> _symbolToIdMap;

    private readonly SemaphoreSlim _lock = new(1, 1);

    public SymbolToIdMappingService(
        IOptions<MappingConfig> mappingConfigOptions,
        ILogger<SymbolToIdMappingService> logger)
    {
        _symbolToIdMapFilePath = mappingConfigOptions.Value.SymbolToIdMapFilePath;
        _logger = logger;
    }

    public async Task<Dictionary<string, long>> GetSymbolToIdMapAsync()
    {
        if (_symbolToIdMap != null && _symbolToIdMap.Count != 0)
            return _symbolToIdMap;

        await _lock.WaitAsync();
        try
        {
            if (_symbolToIdMap != null && _symbolToIdMap.Count != 0)
                return _symbolToIdMap;

            if (File.Exists(_symbolToIdMapFilePath))
            {
                var json = await File.ReadAllTextAsync(_symbolToIdMapFilePath);
                _symbolToIdMap = JsonSerializer.Deserialize<Dictionary<string, long>>(json);
                _logger.LogInformation("Symbol to ID mapping loaded successfully.");
            }
            else
            {
                _logger.LogWarning("Symbol to ID mapping file not found at {FilePath}.", _symbolToIdMapFilePath);
                _symbolToIdMap = [];
            }

            return _symbolToIdMap ?? [];
        }
        finally
        {
            _lock.Release();
        }
    }
}

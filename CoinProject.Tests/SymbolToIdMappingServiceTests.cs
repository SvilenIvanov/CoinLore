namespace CoinLoreTests;

using CoinLore.Configurations;
using CoinLore.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

public class SymbolToIdMappingServiceTests
{
    private readonly Mock<ILogger<SymbolToIdMappingService>> _loggerMock;
    private readonly IOptions<MappingConfig> _mappingConfigOptions;
    private readonly SymbolToIdMappingService _service;
    private readonly string _testFilePath = "symbolToIdMapTest.json";

    public SymbolToIdMappingServiceTests()
    {
        _loggerMock = new Mock<ILogger<SymbolToIdMappingService>>();

        var mappingConfig = new MappingConfig
        {
            SymbolToIdMapFilePath = _testFilePath
        };

        _mappingConfigOptions = Options.Create(mappingConfig);

        _service = new SymbolToIdMappingService(
            _mappingConfigOptions,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task GetSymbolToIdMapAsync_LoadsMappingSuccessfully()
    {
        // Arrange
        var mockMapping = new Dictionary<string, long>
            {
                { "BTC", 1 },
                { "ETH", 2 }
            };

        var json = JsonSerializer.Serialize(mockMapping);
        await File.WriteAllTextAsync(_testFilePath, json);

        // Act
        var result = await _service.GetSymbolToIdMapAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result["BTC"]);
        Assert.Equal(2, result["ETH"]);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Symbol to ID mapping loaded successfully.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once
        );

        // Cleanup
        File.Delete(_testFilePath);
    }

    [Fact]
    public async Task GetSymbolToIdMapAsync_FileNotFound_ReturnsEmptyDictionary()
    {
        // Arrange
        if (File.Exists(_testFilePath))
            File.Delete(_testFilePath);

        // Act
        var result = await _service.GetSymbolToIdMapAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Symbol to ID mapping file not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetSymbolToIdMapAsync_InvalidJson_ThrowsException()
    {
        // Arrange
        var invalidJson = "{ invalid_json }";
        await File.WriteAllTextAsync(_testFilePath, invalidJson);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetSymbolToIdMapAsync());

        Assert.Contains("Symbol to ID mapping file contains invalid JSON.", exception.Message);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to deserialize the symbol to ID mapping file.")),
                It.IsAny<JsonException>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once
        );

        // Cleanup
        File.Delete(_testFilePath);
    }
}
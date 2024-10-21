namespace CoinLoreTests;

using CoinLore.Configurations;
using CoinLore.Exceptions;
using CoinLore.Interfaces;
using CoinLore.Models.CoinLore;
using CoinLore.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

public class CoinMappingServiceTests
{
    private readonly Mock<ICoinLoreClient> _coinLoreClientMock;
    private readonly Mock<ILogger<CoinMappingService>> _loggerMock;
    private readonly IOptions<MappingConfig> _mappingConfigOptions;
    private readonly CoinMappingService _service;
    private readonly string _testFilePath = "symbolToIdMapTest.json";

    public CoinMappingServiceTests()
    {
        _coinLoreClientMock = new Mock<ICoinLoreClient>();
        _loggerMock = new Mock<ILogger<CoinMappingService>>();

        var mappingConfig = new MappingConfig
        {
            SymbolToIdMapFilePath = _testFilePath,
            Limit = 100
        };

        _mappingConfigOptions = Options.Create(mappingConfig);

        _service = new CoinMappingService(
            _coinLoreClientMock.Object,
            _loggerMock.Object,
            _mappingConfigOptions
        );
    }

    [Fact]
    public async Task UpdateCoinMappingAsync_SuccessfullyUpdatesMapping()
    {
        // Arrange
        var globalData = new GlobalData
        {
            CoinsCount = 250
        };

        var coinTickersBatch1 = new List<CoinTicker>
            {
                new CoinTicker { Symbol = "BTC", Id = "1" },
                new CoinTicker { Symbol = "ETH", Id = "2" }
            };

        var coinTickersBatch2 = new List<CoinTicker>
            {
                new CoinTicker { Symbol = "XRP", Id = "3" },
                new CoinTicker { Symbol = "LTC", Id = "4" }
            };

        var coinTickersBatch3 = new List<CoinTicker>
            {
                new CoinTicker { Symbol = "ADA", Id = "5" },
                new CoinTicker { Symbol = "DOT", Id = "6" }
            };

        _coinLoreClientMock.Setup(c => c.GetGlobalDataAsync())
            .ReturnsAsync(globalData);

        _coinLoreClientMock.Setup(c => c.GetTickersByPaginationAsync(0, 100))
            .ReturnsAsync(coinTickersBatch1);

        _coinLoreClientMock.Setup(c => c.GetTickersByPaginationAsync(100, 100))
            .ReturnsAsync(coinTickersBatch2);

        _coinLoreClientMock.Setup(c => c.GetTickersByPaginationAsync(200, 100))
            .ReturnsAsync(coinTickersBatch3);

        // Act
        await _service.UpdateCoinMappingAsync();

        // Assert
        _coinLoreClientMock.Verify(c => c.GetGlobalDataAsync(), Times.Once);
        _coinLoreClientMock.Verify(c => c.GetTickersByPaginationAsync(0, 100), Times.Once);
        _coinLoreClientMock.Verify(c => c.GetTickersByPaginationAsync(100, 100), Times.Once);
        _coinLoreClientMock.Verify(c => c.GetTickersByPaginationAsync(200, 100), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Symbol to ID mapping saved successfully.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once
        );

        var expectedMapping = new Dictionary<string, long>
            {
                { "BTC", 1 },
                { "ETH", 2 },
                { "XRP", 3 },
                { "LTC", 4 },
                { "ADA", 5 },
                { "DOT", 6 }
            };

        var actualJson = await File.ReadAllTextAsync(_testFilePath);
        var actualMapping = JsonSerializer.Deserialize<Dictionary<string, long>>(actualJson);

        Assert.Equal(expectedMapping, actualMapping);
    }

    [Fact]
    public async Task UpdateCoinMappingAsync_GlobalDataIsNull_ThrowsException()
    {
        // Arrange
        _coinLoreClientMock.Setup(c => c.GetGlobalDataAsync())
            .ReturnsAsync((GlobalData)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.UpdateCoinMappingAsync());

        Assert.Equal(400, exception.StatusCode);
        Assert.Equal("Failed to retrieve global data.", exception.Message);
    }

    [Fact]
    public async Task UpdateCoinMappingAsync_InvalidId_LogsWarning()
    {
        // Arrange
        var globalData = new GlobalData
        {
            CoinsCount = 2
        };

        var coinTickersBatch = new List<CoinTicker>
            {
                new CoinTicker { Symbol = "BTC", Id = "abc" }, // Invalid ID
                new CoinTicker { Symbol = "ETH", Id = "2" }
            };

        _coinLoreClientMock.Setup(c => c.GetGlobalDataAsync())
            .ReturnsAsync(globalData);

        _coinLoreClientMock.Setup(c => c.GetTickersByPaginationAsync(0, 100))
            .ReturnsAsync(coinTickersBatch);

        // Act
        await _service.UpdateCoinMappingAsync();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Invalid ID format for symbol BTC")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once
        );

        var actualJson = await File.ReadAllTextAsync(_testFilePath);
        var actualMapping = JsonSerializer.Deserialize<Dictionary<string, long>>(actualJson);

        Assert.Equal(2, actualMapping.Count);
        Assert.Equal(0L, actualMapping["BTC"]); // Assuming handled as 0
        Assert.Equal(2L, actualMapping["ETH"]);
    }
}
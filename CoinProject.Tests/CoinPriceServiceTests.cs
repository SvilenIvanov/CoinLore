namespace CoinLoreTests;

using CoinLore.Interfaces;
using CoinLore.Models.CoinLore;
using CoinLore.Services;
using Microsoft.Extensions.Logging;
using Moq;

public class CoinPriceServiceTests
{
    private readonly Mock<ICoinLoreClient> _coinLoreClientMock;
    private readonly Mock<ISymbolToIdMappingService> _mappingServiceMock;
    private readonly Mock<ILogger<CoinPriceService>> _loggerMock;
    private readonly CoinPriceService _service;

    public CoinPriceServiceTests()
    {
        _coinLoreClientMock = new Mock<ICoinLoreClient>();
        _mappingServiceMock = new Mock<ISymbolToIdMappingService>();
        _loggerMock = new Mock<ILogger<CoinPriceService>>();

        _service = new CoinPriceService(
            _coinLoreClientMock.Object,
            _mappingServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task GetCurrentPricesAsync_ReturnsPrices_ForValidSymbols()
    {
        // Arrange
        var coinSymbols = new List<string> { "BTC", "ETH" };
        var symbolToIdMap = new Dictionary<string, long>
            {
                { "BTC", 1 },
                { "ETH", 2 }
            };

        var expectedIds = new List<string> { "1", "2" };

        var coinTickers = new List<CoinTicker>
            {
                new CoinTicker { Symbol = "BTC", PriceUsd = "34000.50" },
                new CoinTicker { Symbol = "ETH", PriceUsd = "2000.00" }
            };

        _mappingServiceMock.Setup(m => m.GetSymbolToIdMapAsync())
            .ReturnsAsync(symbolToIdMap);

        _coinLoreClientMock.Setup(c => c.GetTickersByIdsAsync(expectedIds))
            .ReturnsAsync(coinTickers);

        // Act
        var prices = await _service.GetCurrentPricesAsync(coinSymbols);

        // Assert
        Assert.NotNull(prices);
        Assert.Equal(2, prices.Count);
        Assert.Equal(34000.50m, prices["BTC"]);
        Assert.Equal(2000.00m, prices["ETH"]);
    }

    [Fact]
    public async Task GetCurrentPricesAsync_NoValidIds_ReturnsEmptyDictionary()
    {
        // Arrange
        var coinSymbols = new List<string> { "INVALID1", "INVALID2" };
        var symbolToIdMap = new Dictionary<string, long>
            {
                { "BTC", 1 },
                { "ETH", 2 }
            };

        _mappingServiceMock.Setup(m => m.GetSymbolToIdMapAsync())
            .ReturnsAsync(symbolToIdMap);

        // Act
        var prices = await _service.GetCurrentPricesAsync(coinSymbols);

        // Assert
        Assert.NotNull(prices);
        Assert.Empty(prices);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No valid coin IDs found for the given symbols.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetCurrentPricesAsync_InvalidPriceParsing_LogsWarningAndSetsDefault()
    {
        // Arrange
        var coinSymbols = new List<string> { "BTC", "ETH" };
        var symbolToIdMap = new Dictionary<string, long>
            {
                { "BTC", 1 },
                { "ETH", 2 }
            };

        var expectedIds = new List<string> { "1", "2" };

        var coinTickers = new List<CoinTicker>
            {
                new CoinTicker { Symbol = "BTC", PriceUsd = "invalid_price" }, // Invalid price
                new CoinTicker { Symbol = "ETH", PriceUsd = "2000.00" }
            };

        _mappingServiceMock.Setup(m => m.GetSymbolToIdMapAsync())
            .ReturnsAsync(symbolToIdMap);

        _coinLoreClientMock.Setup(c => c.GetTickersByIdsAsync(expectedIds))
            .ReturnsAsync(coinTickers);

        // Act
        var prices = await _service.GetCurrentPricesAsync(coinSymbols);

        // Assert
        Assert.NotNull(prices);
        Assert.Equal(2, prices.Count);
        Assert.Equal(0m, prices["BTC"]); // Default value
        Assert.Equal(2000.00m, prices["ETH"]);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Unable to parse price for symbol BTC")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once
        );
    }
}
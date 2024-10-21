namespace CoinLoreTests;

using CoinLore.Interfaces;
using CoinLore.Services;
using Microsoft.Extensions.Logging;
using Moq;

public class PriceUpdateServiceTests
{
    private readonly Mock<ICoinPriceService> _coinPriceServiceMock;
    private readonly Mock<IPortfolioRepository> _portfolioRepositoryMock;
    private readonly Mock<ILogger<PriceUpdateService>> _loggerMock;
    private readonly PriceUpdateService _service;

    public PriceUpdateServiceTests()
    {
        _coinPriceServiceMock = new Mock<ICoinPriceService>();
        _portfolioRepositoryMock = new Mock<IPortfolioRepository>();
        _loggerMock = new Mock<ILogger<PriceUpdateService>>();

        _service = new PriceUpdateService(
            _coinPriceServiceMock.Object,
            _portfolioRepositoryMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task UpdatePricesAsync_SuccessfullyUpdatesPrices()
    {
        // Arrange
        var symbols = new List<string> { "BTC", "ETH" };
        var prices = new Dictionary<string, decimal>
            {
                { "BTC", 35000.00m },
                { "ETH", 2500.00m }
            };

        _portfolioRepositoryMock.Setup(r => r.GetAllSymbols())
            .Returns(symbols);

        _coinPriceServiceMock.Setup(c => c.GetCurrentPricesAsync(symbols))
            .ReturnsAsync(prices);

        // Act
        await _service.UpdatePricesAsync();

        // Assert
        _portfolioRepositoryMock.Verify(r => r.UpdateCurrentPrice("BTC", 35000.00m), Times.Once);
        _portfolioRepositoryMock.Verify(r => r.UpdateCurrentPrice("ETH", 2500.00m), Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Prices updated at")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdatePricesAsync_NoSymbolsToUpdate_LogsInformation()
    {
        // Arrange
        var symbols = new List<string>();

        _portfolioRepositoryMock.Setup(r => r.GetAllSymbols())
            .Returns(symbols);

        // Act
        await _service.UpdatePricesAsync();

        // Assert
        _coinPriceServiceMock.Verify(c => c.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()), Times.Never);
        _portfolioRepositoryMock.Verify(r => r.UpdateCurrentPrice(It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No symbols to update.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdatePricesAsync_PriceNotFoundForSymbol_LogsWarningAndSetsDefault()
    {
        // Arrange
        var symbols = new List<string> { "BTC", "ETH" };
        var prices = new Dictionary<string, decimal>
            {
                { "BTC", 35000.00m }
                // ETH price not provided
            };

        _portfolioRepositoryMock.Setup(r => r.GetAllSymbols())
            .Returns(symbols);

        _coinPriceServiceMock.Setup(c => c.GetCurrentPricesAsync(symbols))
            .ReturnsAsync(prices);

        // Act
        await _service.UpdatePricesAsync();

        // Assert
        _portfolioRepositoryMock.Verify(r => r.UpdateCurrentPrice("BTC", 35000.00m), Times.Once);
        _portfolioRepositoryMock.Verify(r => r.UpdateCurrentPrice("ETH", 0m), Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Price not found for symbol ETH")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once
        );

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Prices updated at")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once
        );
    }
}

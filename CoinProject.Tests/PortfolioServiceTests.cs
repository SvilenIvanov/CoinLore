namespace CoinLoreTests;

using CoinLore.Interfaces;
using CoinLore.Models;
using CoinLore.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;

public class PortfolioServiceTests
{
    private readonly Mock<IPortfolioRepository> _portfolioRepositoryMock;
    private readonly Mock<ISymbolToIdMappingService> _mappingServiceMock;
    private readonly Mock<IPriceUpdateService> _priceUpdateServiceMock;
    private readonly Mock<ILogger<PortfolioService>> _loggerMock;
    private readonly PortfolioService _service;

    public PortfolioServiceTests()
    {
        _portfolioRepositoryMock = new Mock<IPortfolioRepository>();
        _mappingServiceMock = new Mock<ISymbolToIdMappingService>();
        _priceUpdateServiceMock = new Mock<IPriceUpdateService>();
        _loggerMock = new Mock<ILogger<PortfolioService>>();

        _service = new PortfolioService(
            _portfolioRepositoryMock.Object,
            _loggerMock.Object,
            _mappingServiceMock.Object,
            _priceUpdateServiceMock.Object
        );
    }

    [Fact]
    public async Task UploadPortfolioAsync_SuccessfullyUploadsAndUpdatesPrices()
    {
        // Arrange
        var mockFile = CreateMockFormFile("2.5|BTC|34000.50\r\n5|ETH|2000.00");
        var symbolToIdMap = new Dictionary<string, long>
            {
                { "BTC", 1 },
                { "ETH", 2 }
            };

        var expectedItems = new List<PortfolioItem>
            {
                new PortfolioItem { Id = 1, Quantity = 2.5m, Coin = "BTC", InitialPrice = 34000.50m },
                new PortfolioItem { Id = 2, Quantity = 5m, Coin = "ETH", InitialPrice = 2000.00m }
            };

        _mappingServiceMock.Setup(m => m.GetSymbolToIdMapAsync())
            .ReturnsAsync(symbolToIdMap);

        _portfolioRepositoryMock.Setup(r => r.UploadPortfolioAsync(It.IsAny<List<PortfolioItem>>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        _priceUpdateServiceMock.Setup(p => p.UpdatePricesAsync())
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _service.UploadPortfolioAsync(mockFile.Object);

        // Assert
        _portfolioRepositoryMock.Verify(r => r.UploadPortfolioAsync(It.Is<List<PortfolioItem>>(list =>
            list.Count == 2 &&
            list[0].Id == 1 &&
            list[0].Quantity == 2.5m &&
            list[0].Coin == "BTC" &&
            list[0].InitialPrice == 34000.50m &&
            list[1].Id == 2 &&
            list[1].Quantity == 5m &&
            list[1].Coin == "ETH" &&
            list[1].InitialPrice == 2000.00m
        )), Times.Once);

        _priceUpdateServiceMock.Verify(p => p.UpdatePricesAsync(), Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Portfolio uploaded and parsed successfully.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once
        );

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Prices updated immediately after portfolio upload.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task UploadPortfolioAsync_InvalidFile_ThrowsException()
    {
        // Arrange
        IFormFile nullFile = null;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.UploadPortfolioAsync(nullFile));

        Assert.Equal("Invalid file.", exception.Message);
    }

    [Fact]
    public async Task UploadPortfolioAsync_NoValidItems_ThrowsException()
    {
        // Arrange
        var mockFile = CreateMockFormFile("invalid_line\r\nanother_invalid_line");
        var symbolToIdMap = new Dictionary<string, long>
            {
                { "BTC", 1 },
                { "ETH", 2 }
            };

        _mappingServiceMock.Setup(m => m.GetSymbolToIdMapAsync())
            .ReturnsAsync(symbolToIdMap);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UploadPortfolioAsync(mockFile.Object));

        Assert.Equal("No valid portfolio items found in the uploaded file.", exception.Message);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No valid portfolio items were parsed from the uploaded file.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task UploadPortfolioAsync_SymbolNotFound_LogsWarning()
    {
        // Arrange
        var mockFile = CreateMockFormFile("2.5|BTC|34000.50\r\n5|UNKNOWN|2000.00");
        var symbolToIdMap = new Dictionary<string, long>
            {
                { "BTC", 1 },
                { "ETH", 2 }
            };

        var expectedItems = new List<PortfolioItem>
            {
                new PortfolioItem { Id = 1, Quantity = 2.5m, Coin = "BTC", InitialPrice = 34000.50m }
            };

        _mappingServiceMock.Setup(m => m.GetSymbolToIdMapAsync())
            .ReturnsAsync(symbolToIdMap);

        _portfolioRepositoryMock.Setup(r => r.UploadPortfolioAsync(It.IsAny<List<PortfolioItem>>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        _priceUpdateServiceMock.Setup(p => p.UpdatePricesAsync())
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _service.UploadPortfolioAsync(mockFile.Object);

        // Assert
        _portfolioRepositoryMock.Verify(r => r.UploadPortfolioAsync(It.Is<List<PortfolioItem>>(list =>
            list.Count == 1 &&
            list[0].Id == 1 &&
            list[0].Quantity == 2.5m &&
            list[0].Coin == "BTC" &&
            list[0].InitialPrice == 34000.50m
        )), Times.Once);

        _priceUpdateServiceMock.Verify(p => p.UpdatePricesAsync(), Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Coin symbol UNKNOWN not found in mapping at line 2. Skipping.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once
        );
    }

    private Mock<IFormFile> CreateMockFormFile(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(bytes.Length);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
        mockFile.Setup(f => f.FileName).Returns("portfolio.txt");
        mockFile.Setup(f => f.ContentType).Returns("text/plain");

        return mockFile;
    }
}

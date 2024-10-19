namespace CoinLore.Controllers;

using Interfaces;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/[controller]")]
public class PortfolioController : ControllerBase
{
    private readonly IPortfolioService _portfolioService;

    public PortfolioController(IPortfolioService portfolioService)
    {
        _portfolioService = portfolioService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadPortfolio([FromForm] IFormFile file)
    {
        await _portfolioService.UploadPortfolioAsync(file);
        return Ok(new { Message = "Portfolio uploaded successfully." });
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetPortfolioSummary()
    {
        var summary = await _portfolioService.GetPortfolioSummaryAsync();
        return Ok(summary);
    }
}
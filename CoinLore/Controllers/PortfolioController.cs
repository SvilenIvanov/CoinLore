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

    /// <summary>
    /// Uploads a portfolio file. Saves it in memory.
    /// </summary>
    /// <param name="file">The portfolio file to upload.</param>
    /// <returns>A confirmation message.</returns>
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadPortfolio(IFormFile file)
    {
        await _portfolioService.UploadPortfolioAsync(file);
        return Ok(new { Message = "Portfolio uploaded successfully." });
    }


    /// <summary>
    /// Fetches information about the portfolio profile.
    /// </summary>
    /// <returns></returns>
    [HttpGet("summary")]
    public async Task<IActionResult> GetPortfolioSummary()
    {
        var summary = await _portfolioService.GetPortfolioSummaryAsync();
        return Ok(summary);
    }
}
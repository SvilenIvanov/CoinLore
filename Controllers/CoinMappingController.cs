namespace CoinLore.Controllers;

using Microsoft.AspNetCore.Mvc;
using Services;

[ApiController]
[Route("api/[controller]")]
public class CoinMappingController : ControllerBase
{
    private readonly ICoinMappingService _coinMappingService;

    public CoinMappingController(ICoinMappingService coinMappingService)
    {
        _coinMappingService = coinMappingService;
    }

    [HttpPost("update")]
    public async Task<IActionResult> UpdateCoinMapping()
    {
        await _coinMappingService.UpdateCoinMappingAsync();
        return Ok("Symbol to ID mapping updated successfully.");
    }
}
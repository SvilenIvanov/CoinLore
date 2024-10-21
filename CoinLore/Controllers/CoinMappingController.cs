namespace CoinLore.Controllers;

using Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CoinMappingController : ControllerBase
{
    private readonly ICoinMappingService _coinMappingService;

    public CoinMappingController(ICoinMappingService coinMappingService)
    {
        _coinMappingService = coinMappingService;
    }

    /// <summary>
    /// Creates a mapping between cryptocurrency coin and id from the coinlore api.
    /// Saves it in the Mapping directory.
    /// The mapping is used for the Portfolio endpoints
    /// </summary>
    /// <returns></returns>
    [HttpPost("update")]
    public async Task<IActionResult> UpdateCoinMapping()
    {
        await _coinMappingService.UpdateCoinMappingAsync();
        return Ok("Symbol to ID mapping updated successfully.");
    }
}
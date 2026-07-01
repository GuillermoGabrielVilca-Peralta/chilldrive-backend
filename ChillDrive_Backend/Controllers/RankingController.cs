using ChillDrive_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChillDrive_Backend.Controllers;

// Leaderboard de conductores (vista v_ranking_conductores).
// Público: la web informativa lo consume sin token vía BFF.
public class RankingController : ApiControllerBase
{
    private readonly IRankingService _service;

    public RankingController(IRankingService service)
    {
        _service = service;
    }

    // GET api/ranking — público (leaderboard de acceso libre)
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var data = await _service.GetRanking();
        return Ok(data);
    }
}

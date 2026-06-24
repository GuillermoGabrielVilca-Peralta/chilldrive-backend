using ChillDrive_Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChillDrive_Backend.Controllers;

// Leaderboard de conductores (vista v_ranking_conductores).
// Visible para todos los usuarios autenticados (es un ranking público dentro de la app).
public class RankingController : ApiControllerBase
{
    private readonly IRankingService _service;

    public RankingController(IRankingService service)
    {
        _service = service;
    }

    // GET api/ranking
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var data = await _service.GetRanking();
        return Ok(data);
    }
}

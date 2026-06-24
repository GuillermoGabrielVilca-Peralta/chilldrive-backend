using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChillDrive_Backend.Controllers;

// Reporte de suscripciones activas (vista v_suscripciones_activas). Solo administradores.
public class SuscripcionActivaController : ApiControllerBase
{
    private readonly ISuscripcionActivaService _service;

    public SuscripcionActivaController(ISuscripcionActivaService service)
    {
        _service = service;
    }

    // GET api/suscripcionactiva  → solo administradores
    [Authorize(Roles = Roles.Admin)]
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var data = await _service.GetActivas();
        return Ok(data);
    }
}

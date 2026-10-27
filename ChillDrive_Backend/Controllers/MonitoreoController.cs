using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChillDrive_Backend.Controllers;

// Panel de administración: vehículos en ruta para el mapa en vivo.
[Authorize(Roles = Roles.Admin)]
public class MonitoreoController : ApiControllerBase
{
    private readonly IMonitoreoService _service;

    public MonitoreoController(IMonitoreoService service)
    {
        _service = service;
    }

    // GET api/monitoreo/viajes-activos  → solo administradores
    [HttpGet("viajes-activos")]
    public async Task<IActionResult> GetViajesActivos()
    {
        var data = await _service.GetViajesActivos();
        return Ok(data);
    }
}

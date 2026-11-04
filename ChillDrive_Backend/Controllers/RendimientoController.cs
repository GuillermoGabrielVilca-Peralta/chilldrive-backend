using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChillDrive_Backend.Controllers;

// Panel de administración: análisis de rendimiento de la flota.
[Authorize(Roles = Roles.Admin)]
public class RendimientoController : ApiControllerBase
{
    private readonly IRendimientoService _service;

    public RendimientoController(IRendimientoService service)
    {
        _service = service;
    }

    // GET api/rendimiento?dias=30&kmPorGalon=40&precioGalon=16  → solo administradores
    // kmPorGalon y precioGalon son de la flota y solo afectan el combustible estimado.
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int dias = 30,
        [FromQuery] double kmPorGalon  = EficienciaCombustible.KmPorGalonPorDefecto,
        [FromQuery] double precioGalon = EficienciaCombustible.PrecioGalonPorDefecto)
    {
        var (success, mensaje, data) = await _service.Get(dias, kmPorGalon, precioGalon);
        if (!success) return BadRequest(new { mensaje });
        return Ok(data);
    }
}

using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChillDrive_Backend.Controllers;

// Los eventos los generan las cámaras/IA. La pertenencia es indirecta (evento → cámara/viaje
// → usuario); por ahora las consultas quedan autenticadas y la administración (listar todo,
// eliminar) restringida a admin. Pendiente: scoping por dueño cuando se defina la auth de dispositivos.
public class EventoController : ApiControllerBase
{
    private readonly IEventoService _service;

    public EventoController(IEventoService service)
    {
        _service = service;
    }

    // GET api/evento  → solo administradores
    [Authorize(Roles = Roles.Admin)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _service.GetAll();
        return Ok(data);
    }

    // GET api/evento/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var evento = await _service.GetById(id);
        if (evento is null)
            return NotFound(new { mensaje = "Evento no encontrado." });
        return Ok(evento);
    }

    // GET api/evento/viaje/5?pagina=1&tamano=50
    // Con parámetros devuelve PaginadoDto; sin ellos devuelve la lista completa (retrocompatible).
    [HttpGet("viaje/{viajeId:int}")]
    public async Task<IActionResult> GetByViaje(
        int viajeId,
        [FromQuery] int? pagina,
        [FromQuery] int? tamano)
    {
        if (pagina.HasValue || tamano.HasValue)
        {
            var p = Math.Max(1, pagina ?? 1);
            var t = Math.Clamp(tamano ?? 50, 1, 200);
            return Ok(await _service.GetByViajeIdPaginado(viajeId, p, t));
        }

        return Ok(await _service.GetByViajeId(viajeId));
    }

    // GET api/evento/camara/5  → todos los eventos de una cámara
    [HttpGet("camara/{camaraId:int}")]
    public async Task<IActionResult> GetByCamara(int camaraId)
    {
        var data = await _service.GetByCamaraId(camaraId);
        return Ok(data);
    }

    // POST api/evento  → registrar detección de IA
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] EventoRequestDto dto)
    {
        var (success, mensaje, id) = await _service.Registrar(dto);
        if (!success)
            return BadRequest(new { mensaje });
        return CreatedAtAction(nameof(GetById), new { id }, new { mensaje, id });
    }

    // DELETE api/evento/5  → solo administradores
    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, mensaje) = await _service.Eliminar(id);
        if (!success)
            return NotFound(new { mensaje });
        return Ok(new { mensaje });
    }
}
using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChillDrive_Backend.Controllers;

// Catálogo de premios: lectura para cualquier usuario autenticado, escritura solo admin.
public class PremioController : ApiControllerBase
{
    private readonly IPremioService _service;

    public PremioController(IPremioService service)
    {
        _service = service;
    }

    // GET api/premio
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _service.GetAll();
        return Ok(data);
    }

    // GET api/premio/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var premio = await _service.GetById(id);
        if (premio is null)
            return NotFound(new { mensaje = "Premio no encontrado." });
        return Ok(premio);
    }

    // GET api/premio/disponibles?pagina=1&tamano=20
    // Con parámetros devuelve PaginadoDto; sin ellos devuelve la lista completa (retrocompatible).
    [HttpGet("disponibles")]
    public async Task<IActionResult> GetDisponibles(
        [FromQuery] int? pagina,
        [FromQuery] int? tamano)
    {
        if (pagina.HasValue || tamano.HasValue)
        {
            var p = Math.Max(1, pagina ?? 1);
            var t = Math.Clamp(tamano ?? 20, 1, 100);
            return Ok(await _service.GetDisponiblesPaginado(p, t));
        }

        return Ok(await _service.GetDisponibles());
    }

    // GET api/premio/aliado/5  → premios de un aliado
    [HttpGet("aliado/{aliadoId:int}")]
    public async Task<IActionResult> GetByAliado(int aliadoId)
    {
        var data = await _service.GetByAliadoId(aliadoId);
        return Ok(data);
    }

    // POST api/premio  → solo administradores
    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] PremioRequestDto dto)
    {
        var (success, mensaje, id) = await _service.Registrar(dto);
        if (!success)
            return BadRequest(new { mensaje });
        return CreatedAtAction(nameof(GetById), new { id }, new { mensaje, id });
    }

    // PUT api/premio/5  → solo administradores
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, [FromBody] PremioRequestDto dto)
    {
        var (success, mensaje) = await _service.Actualizar(id, dto);
        if (!success)
            return BadRequest(new { mensaje });
        return Ok(new { mensaje });
    }

    // DELETE api/premio/5  → solo administradores
    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, mensaje) = await _service.Eliminar(id);
        if (!success)
            return NotFound(new { mensaje });
        return Ok(new { mensaje });
    }

    // PATCH api/premio/5/estado  → pausar / activar / marcar agotado (solo administradores)
    [Authorize(Roles = Roles.Admin)]
    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] string nuevoEstado)
    {
        var (success, mensaje) = await _service.CambiarEstado(id, nuevoEstado);
        if (!success)
            return BadRequest(new { mensaje });
        return Ok(new { mensaje });
    }
}
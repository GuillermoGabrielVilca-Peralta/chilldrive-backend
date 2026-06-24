using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChillDrive_Backend.Controllers;

// Catálogo de aliados: lectura para cualquier usuario autenticado, escritura solo admin.
public class AliadoController : ApiControllerBase
{
    private readonly IAliadoService _service;

    public AliadoController(IAliadoService service)
    {
        _service = service;
    }

    // GET api/aliado
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _service.GetAll();
        return Ok(data);
    }

    // GET api/aliado/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var aliado = await _service.GetById(id);
        if (aliado is null)
            return NotFound(new { mensaje = "Aliado no encontrado." });
        return Ok(aliado);
    }

    // GET api/aliado/categoria/Gastronomia
    [HttpGet("categoria/{categoria}")]
    public async Task<IActionResult> GetByCategoria(string categoria)
    {
        var data = await _service.GetByCategoria(categoria);
        return Ok(data);
    }

    // POST api/aliado  → solo administradores
    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] AliadoRequestDto dto)
    {
        var (success, mensaje, id) = await _service.Registrar(dto);
        if (!success)
            return Conflict(new { mensaje });
        return CreatedAtAction(nameof(GetById), new { id }, new { mensaje, id });
    }

    // PUT api/aliado/5  → solo administradores
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, [FromBody] AliadoRequestDto dto)
    {
        var (success, mensaje) = await _service.Actualizar(id, dto);
        if (!success)
            return BadRequest(new { mensaje });
        return Ok(new { mensaje });
    }

    // DELETE api/aliado/5  → solo administradores
    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, mensaje) = await _service.Eliminar(id);
        if (!success)
            return NotFound(new { mensaje });
        return Ok(new { mensaje });
    }

    // PATCH api/aliado/5/estado  → activar / suspender aliado (solo administradores)
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
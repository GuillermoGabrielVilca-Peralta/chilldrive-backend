using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChillDrive_Backend.Controllers;

public class SuscripcionController : ApiControllerBase
{
    private readonly ISuscripcionService _service;

    public SuscripcionController(ISuscripcionService service)
    {
        _service = service;
    }

    // GET api/suscripcion  → solo administradores
    [Authorize(Roles = Roles.Admin)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _service.GetAll();
        return Ok(data);
    }

    // GET api/suscripcion/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var s = await _service.GetById(id);
        if (s is null)
            return NotFound(new { mensaje = "Suscripción no encontrada." });

        // Pertenencia: solo el dueño puede ver la suscripción.
        if (s.UsuarioId != UsuarioActualId)
            return Forbid();

        return Ok(s);
    }

    // GET api/suscripcion/usuario/5  → historial de suscripciones de un usuario
    [HttpGet("usuario/{usuarioId:int}")]
    public async Task<IActionResult> GetByUsuario(int usuarioId)
    {
        // Pertenencia: solo puedes listar tus propias suscripciones.
        if (usuarioId != UsuarioActualId)
            return Forbid();

        var data = await _service.GetByUsuarioId(usuarioId);
        return Ok(data);
    }

    // GET api/suscripcion/usuario/5/activa  → suscripción vigente
    [HttpGet("usuario/{usuarioId:int}/activa")]
    public async Task<IActionResult> GetActiva(int usuarioId)
    {
        // Pertenencia: solo puedes consultar tu propia suscripción activa.
        if (usuarioId != UsuarioActualId)
            return Forbid();

        var s = await _service.GetActivaByUsuarioId(usuarioId);
        if (s is null)
            return NotFound(new { mensaje = "El usuario no tiene suscripción activa." });
        return Ok(s);
    }

    // POST api/suscripcion  → registrar pago y activar plan Profesional
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] SuscripcionRequestDto dto)
    {
        // Pertenencia: solo puedes registrar tu propia suscripción.
        if (dto.UsuarioId != UsuarioActualId)
            return Forbid();

        var (success, mensaje, id) = await _service.Registrar(dto);
        if (!success)
            return BadRequest(new { mensaje });
        return CreatedAtAction(nameof(GetById), new { id }, new { mensaje, id });
    }

    // PATCH api/suscripcion/5/cancelar  → cancelar y revertir plan a Gratuito
    [HttpPatch("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id)
    {
        // Pertenencia: solo el dueño puede cancelar su suscripción.
        var s = await _service.GetById(id);
        if (s is null)
            return NotFound(new { mensaje = "Suscripción no encontrada." });
        if (s.UsuarioId != UsuarioActualId)
            return Forbid();

        var (success, mensaje) = await _service.Cancelar(id);
        if (!success)
            return BadRequest(new { mensaje });
        return Ok(new { mensaje });
    }
}
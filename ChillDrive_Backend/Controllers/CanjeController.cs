using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChillDrive_Backend.Controllers;

public class CanjeController : ApiControllerBase
{
    private readonly ICanjeService _service;

    public CanjeController(ICanjeService service)
    {
        _service = service;
    }

    // GET api/canje  → solo administradores
    [Authorize(Roles = Roles.Admin)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _service.GetAll();
        return Ok(data);
    }

    // GET api/canje/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var canje = await _service.GetById(id);
        if (canje is null)
            return NotFound(new { mensaje = "Canje no encontrado." });

        // Pertenencia: solo el dueño puede ver el canje.
        if (canje.UsuarioId != UsuarioActualId)
            return Forbid();

        return Ok(canje);
    }

    // GET api/canje/usuario/5  → historial de canjes de un usuario
    [HttpGet("usuario/{usuarioId:int}")]
    public async Task<IActionResult> GetByUsuario(int usuarioId)
    {
        // Pertenencia: solo puedes listar tus propios canjes.
        if (usuarioId != UsuarioActualId)
            return Forbid();

        var data = await _service.GetByUsuarioId(usuarioId);
        return Ok(data);
    }

    // POST api/canje  → canjear premio con puntos
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CanjeRequestDto dto)
    {
        // Pertenencia: solo puedes canjear con tus propios puntos.
        if (dto.UsuarioId != UsuarioActualId)
            return Forbid();

        var (success, mensaje, id) = await _service.Canjear(dto);
        if (!success)
            return BadRequest(new { mensaje });
        return CreatedAtAction(nameof(GetById), new { id }, new { mensaje, id });
    }

    // PATCH api/canje/qr/{codigoQr}  → el aliado escanea el QR en el negocio
    // Lo valida un administrador (no el dueño del canje). Cuando exista un rol "Aliado"
    // con login propio, podría ampliarse a ese rol.
    [Authorize(Roles = Roles.Admin)]
    [HttpPatch("qr/{codigoQr}")]
    public async Task<IActionResult> UsarQr(string codigoQr)
    {
        var (success, mensaje) = await _service.UsarQr(codigoQr);
        if (!success)
            return BadRequest(new { mensaje });
        return Ok(new { mensaje });
    }

    // PATCH api/canje/5/anular  → anular canje y devolver puntos
    [HttpPatch("{id:int}/anular")]
    public async Task<IActionResult> Anular(int id)
    {
        // Pertenencia: solo el dueño puede anular su canje.
        var canje = await _service.GetById(id);
        if (canje is null)
            return NotFound(new { mensaje = "Canje no encontrado." });
        if (canje.UsuarioId != UsuarioActualId)
            return Forbid();

        var (success, mensaje) = await _service.Anular(id);
        if (!success)
            return BadRequest(new { mensaje });
        return Ok(new { mensaje });
    }
}

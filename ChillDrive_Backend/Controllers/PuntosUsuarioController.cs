using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChillDrive_Backend.Controllers;

public class PuntosUsuarioController : ApiControllerBase
{
    private readonly IPuntosUsuarioService _service;

    public PuntosUsuarioController(IPuntosUsuarioService service)
    {
        _service = service;
    }

    // El ranking ahora vive en RankingController (GET /api/ranking), que usa la vista
    // v_ranking_conductores con datos más ricos (nombre, puntaje promedio, total de viajes).

    // GET api/puntosusuario/usuario/5  → saldo y nivel de un usuario
    [HttpGet("usuario/{usuarioId:int}")]
    public async Task<IActionResult> GetByUsuario(int usuarioId)
    {
        // Pertenencia: solo puedes consultar tu propio saldo.
        if (usuarioId != UsuarioActualId)
            return Forbid();

        var puntos = await _service.GetByUsuarioId(usuarioId);
        if (puntos is null)
            return NotFound(new { mensaje = "El usuario no tiene puntos registrados." });
        return Ok(puntos);
    }

    // POST api/puntosusuario/acreditar  → acreditar puntos manualmente (operación administrativa)
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("acreditar")]
    public async Task<IActionResult> Acreditar([FromBody] AcreditarPuntosRequestDto dto)
    {
        var (success, mensaje) = await _service.AcreditarPuntos(dto.UsuarioId, dto.Puntos);
        if (!success)
            return BadRequest(new { mensaje });
        return Ok(new { mensaje });
    }
}

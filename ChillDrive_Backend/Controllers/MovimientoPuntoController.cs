using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChillDrive_Backend.Controllers;

public class MovimientoPuntoController : ApiControllerBase
{
    private readonly IMovimientoPuntoService _service;

    public MovimientoPuntoController(IMovimientoPuntoService service)
    {
        _service = service;
    }

    // GET api/movimientopunto/usuario/5
    // Historial completo de puntos de un usuario
    [HttpGet("usuario/{usuarioId:int}")]
    public async Task<IActionResult> GetByUsuario(int usuarioId)
    {
        // Pertenencia: solo puedes ver tu propio historial de puntos.
        if (usuarioId != UsuarioActualId)
            return Forbid();

        var data = await _service.GetByUsuarioId(usuarioId);
        return Ok(data);
    }

    // GET api/movimientopunto/usuario/5/ganados
    // Solo puntos ganados por viajes
    [HttpGet("usuario/{usuarioId:int}/ganados")]
    public async Task<IActionResult> GetGanados(int usuarioId)
    {
        if (usuarioId != UsuarioActualId)
            return Forbid();

        var data = await _service.GetByUsuarioIdYTipo(usuarioId, MovimientoTipos.Ganado);
        return Ok(data);
    }

    // GET api/movimientopunto/usuario/5/canjeados
    // Solo puntos gastados en canjes
    [HttpGet("usuario/{usuarioId:int}/canjeados")]
    public async Task<IActionResult> GetCanjeados(int usuarioId)
    {
        if (usuarioId != UsuarioActualId)
            return Forbid();

        var data = await _service.GetByUsuarioIdYTipo(usuarioId, MovimientoTipos.Canjeado);
        return Ok(data);
    }

    // GET api/movimientopunto/5
    // Un movimiento específico
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var movimiento = await _service.GetById(id);
        if (movimiento is null)
            return NotFound(new { mensaje = "Movimiento no encontrado." });

        // Pertenencia: solo el dueño puede ver el movimiento.
        if (movimiento.UsuarioId != UsuarioActualId)
            return Forbid();

        return Ok(movimiento);
    }
}

using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChillDrive_Backend.Controllers;

public class ViajeController : ApiControllerBase
{
    private readonly IViajeService    _service;
    private readonly IVehiculoService _vehiculoService;

    // IVehiculoService se inyecta para resolver el dueño en consultas por vehículo.
    public ViajeController(IViajeService service, IVehiculoService vehiculoService)
    {
        _service         = service;
        _vehiculoService = vehiculoService;
    }

    // GET api/viaje  → solo administradores
    [Authorize(Roles = Roles.Admin)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _service.GetAll();
        return Ok(data);
    }

    // GET api/viaje/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var viaje = await _service.GetById(id);
        if (viaje is null)
            return NotFound(new { mensaje = "Viaje no encontrado." });

        // Pertenencia: solo el dueño puede ver el viaje.
        if (viaje.UsuarioId != UsuarioActualId)
            return Forbid();

        return Ok(viaje);
    }

    // GET api/viaje/usuario/5?pagina=1&tamano=20
    // Con parámetros devuelve PaginadoDto; sin ellos devuelve la lista completa (retrocompatible).
    [HttpGet("usuario/{usuarioId:int}")]
    public async Task<IActionResult> GetByUsuario(
        int usuarioId,
        [FromQuery] int? pagina,
        [FromQuery] int? tamano)
    {
        if (usuarioId != UsuarioActualId)
            return Forbid();

        if (pagina.HasValue || tamano.HasValue)
        {
            var p = Math.Max(1, pagina ?? 1);
            var t = Math.Clamp(tamano ?? 20, 1, 100);
            return Ok(await _service.GetByUsuarioIdPaginado(usuarioId, p, t));
        }

        return Ok(await _service.GetByUsuarioId(usuarioId));
    }

    // GET api/viaje/vehiculo/5
    [HttpGet("vehiculo/{vehiculoId:int}")]
    public async Task<IActionResult> GetByVehiculo(int vehiculoId)
    {
        // Pertenencia indirecta: el vehículo debe ser tuyo.
        var vehiculo = await _vehiculoService.GetById(vehiculoId);
        if (vehiculo is null)
            return NotFound(new { mensaje = "Vehículo no encontrado." });
        if (vehiculo.UsuarioId != UsuarioActualId)
            return Forbid();

        var data = await _service.GetByVehiculoId(vehiculoId);
        return Ok(data);
    }

    // POST api/viaje  → inicia un viaje nuevo
    [HttpPost]
    public async Task<IActionResult> Iniciar([FromBody] ViajeRequestDto dto)
    {
        // Pertenencia: solo puedes iniciar viajes a tu propio nombre.
        if (dto.UsuarioId != UsuarioActualId)
            return Forbid();

        var (success, mensaje, id) = await _service.Iniciar(dto);
        if (!success)
            return BadRequest(new { mensaje });
        return CreatedAtAction(nameof(GetById), new { id }, new { mensaje, id });
    }

    // PATCH api/viaje/5/finalizar  → endpoint específico de negocio
    [HttpPatch("{id:int}/finalizar")]
    public async Task<IActionResult> Finalizar(int id, [FromBody] FinalizarViajeRequestDto dto)
    {
        if (await NoEsMiViaje(id) is { } forbidden)
            return forbidden;

        var (success, mensaje) = await _service.Finalizar(id, dto);
        if (!success)
            return BadRequest(new { mensaje });
        return Ok(new { mensaje });
    }

    // PATCH api/viaje/5/cancelar
    [HttpPatch("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id)
    {
        if (await NoEsMiViaje(id) is { } forbidden)
            return forbidden;

        var (success, mensaje) = await _service.Cancelar(id);
        if (!success)
            return BadRequest(new { mensaje });
        return Ok(new { mensaje });
    }

    // DELETE api/viaje/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (await NoEsMiViaje(id) is { } forbidden)
            return forbidden;

        var (success, mensaje) = await _service.Eliminar(id);
        if (!success)
            return NotFound(new { mensaje });
        return Ok(new { mensaje });
    }

    // Verifica pertenencia para operaciones sobre un viaje existente.
    // Devuelve NotFound/Forbid si corresponde, o null si el viaje es del usuario.
    private async Task<IActionResult?> NoEsMiViaje(int id)
    {
        var viaje = await _service.GetById(id);
        if (viaje is null)
            return NotFound(new { mensaje = "Viaje no encontrado." });
        if (viaje.UsuarioId != UsuarioActualId)
            return Forbid();
        return null;
    }
}

using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChillDrive_Backend.Controllers;

public class CamaraController : ApiControllerBase
{
    private readonly ICamaraService   _service;
    private readonly IVehiculoService _vehiculoService;

    // IVehiculoService se inyecta para resolver el dueño: una cámara pertenece al
    // usuario a través de su vehículo (cámara → vehículo → usuario).
    public CamaraController(ICamaraService service, IVehiculoService vehiculoService)
    {
        _service         = service;
        _vehiculoService = vehiculoService;
    }

    // GET api/camara  → solo administradores
    [Authorize(Roles = Roles.Admin)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _service.GetAll();
        return Ok(data);
    }

    // GET api/camara/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var camara = await _service.GetById(id);
        if (camara is null)
            return NotFound(new { mensaje = "Cámara no encontrada." });

        // Pertenencia indirecta: el vehículo de la cámara debe ser tuyo.
        if (!await VehiculoEsMio(camara.VehiculoId))
            return Forbid();

        return Ok(camara);
    }

    // GET api/camara/vehiculo/5  → todas las cámaras de un vehículo
    [HttpGet("vehiculo/{vehiculoId:int}")]
    public async Task<IActionResult> GetByVehiculo(int vehiculoId)
    {
        var vehiculo = await _vehiculoService.GetById(vehiculoId);
        if (vehiculo is null)
            return NotFound(new { mensaje = "Vehículo no encontrado." });
        if (vehiculo.UsuarioId != UsuarioActualId)
            return Forbid();

        var data = await _service.GetByVehiculoId(vehiculoId);
        return Ok(data);
    }

    // POST api/camara
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CamaraRequestDto dto)
    {
        // Pertenencia: solo puedes registrar cámaras en tus propios vehículos.
        // Si el vehículo no existe, deja que el servicio devuelva su mensaje específico.
        var vehiculo = await _vehiculoService.GetById(dto.VehiculoId);
        if (vehiculo is not null && vehiculo.UsuarioId != UsuarioActualId)
            return Forbid();

        var (success, mensaje, id) = await _service.Registrar(dto);
        if (!success)
            return BadRequest(new { mensaje });
        return CreatedAtAction(nameof(GetById), new { id }, new { mensaje, id });
    }

    // PUT api/camara/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, [FromBody] CamaraRequestDto dto)
    {
        if (await NoEsMiCamara(id) is { } forbidden)
            return forbidden;

        // Tampoco puedes mover la cámara a un vehículo que no es tuyo.
        var destino = await _vehiculoService.GetById(dto.VehiculoId);
        if (destino is not null && destino.UsuarioId != UsuarioActualId)
            return Forbid();

        var (success, mensaje) = await _service.Actualizar(id, dto);
        if (!success)
            return BadRequest(new { mensaje });
        return Ok(new { mensaje });
    }

    // DELETE api/camara/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (await NoEsMiCamara(id) is { } forbidden)
            return forbidden;

        var (success, mensaje) = await _service.Eliminar(id);
        if (!success)
            return NotFound(new { mensaje });
        return Ok(new { mensaje });
    }

    // PATCH api/camara/5/estado  → endpoint específico: cambiar estado de conexión
    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] string nuevoEstado)
    {
        if (await NoEsMiCamara(id) is { } forbidden)
            return forbidden;

        var (success, mensaje) = await _service.CambiarEstado(id, nuevoEstado);
        if (!success)
            return BadRequest(new { mensaje });
        return Ok(new { mensaje });
    }

    // Devuelve true si el vehículo existe y pertenece al usuario autenticado.
    private async Task<bool> VehiculoEsMio(int vehiculoId)
    {
        var vehiculo = await _vehiculoService.GetById(vehiculoId);
        return vehiculo is not null && vehiculo.UsuarioId == UsuarioActualId;
    }

    // Para operaciones sobre una cámara existente: 404 si no existe,
    // Forbid si su vehículo no es del usuario, o null si todo está en orden.
    private async Task<IActionResult?> NoEsMiCamara(int camaraId)
    {
        var camara = await _service.GetById(camaraId);
        if (camara is null)
            return NotFound(new { mensaje = "Cámara no encontrada." });
        if (!await VehiculoEsMio(camara.VehiculoId))
            return Forbid();
        return null;
    }
}

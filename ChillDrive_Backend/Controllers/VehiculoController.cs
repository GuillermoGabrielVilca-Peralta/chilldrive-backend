using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChillDrive_Backend.Controllers;

// Hereda [ApiController], la ruta y UsuarioActualId de ApiControllerBase.
public class VehiculoController : ApiControllerBase
{
    private readonly IVehiculoService _service;

    public VehiculoController(IVehiculoService service)
    {
        _service = service;
    }

    // GET api/vehiculo  → solo administradores
    [Authorize(Roles = Roles.Admin)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _service.GetAll();
        return Ok(data);
    }

    // GET api/vehiculo/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var vehiculo = await _service.GetById(id);
        if (vehiculo is null)
            return NotFound(new { mensaje = "Vehículo no encontrado." });

        // Pertenencia: solo el dueño puede ver el vehículo.
        if (vehiculo.UsuarioId != UsuarioActualId)
            return Forbid();

        return Ok(vehiculo);
    }

    // GET api/vehiculo/usuario/5  → todos los vehículos de un usuario
    [HttpGet("usuario/{usuarioId:int}")]
    public async Task<IActionResult> GetByUsuario(int usuarioId)
    {
        // Pertenencia: solo puedes listar tus propios vehículos.
        if (usuarioId != UsuarioActualId)
            return Forbid();

        var data = await _service.GetByUsuarioId(usuarioId);
        return Ok(data);
    }

    // POST api/vehiculo
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] VehiculoRequestDto dto)
    {
        // Pertenencia: solo puedes registrar vehículos a tu propio nombre.
        if (dto.UsuarioId != UsuarioActualId)
            return Forbid();

        var (success, mensaje, id) = await _service.Registrar(dto);
        if (!success)
            return Conflict(new { mensaje });
        return CreatedAtAction(nameof(GetById), new { id }, new { mensaje, id });
    }

    // PUT api/vehiculo/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, [FromBody] VehiculoRequestDto dto)
    {
        // Pertenencia: solo el dueño puede editar el vehículo.
        var vehiculo = await _service.GetById(id);
        if (vehiculo is null)
            return NotFound(new { mensaje = "Vehículo no encontrado." });
        if (vehiculo.UsuarioId != UsuarioActualId)
            return Forbid();

        var (success, mensaje) = await _service.Actualizar(id, dto);
        if (!success)
            return BadRequest(new { mensaje });
        return Ok(new { mensaje });
    }

    // DELETE api/vehiculo/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        // Pertenencia: solo el dueño puede eliminar el vehículo.
        var vehiculo = await _service.GetById(id);
        if (vehiculo is null)
            return NotFound(new { mensaje = "Vehículo no encontrado." });
        if (vehiculo.UsuarioId != UsuarioActualId)
            return Forbid();

        var (success, mensaje) = await _service.Eliminar(id);
        if (!success)
            return NotFound(new { mensaje });
        return Ok(new { mensaje });
    }
}
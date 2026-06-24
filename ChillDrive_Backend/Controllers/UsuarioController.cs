using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChillDrive_Backend.Controllers;

// Hereda [ApiController] y la ruta "api/[controller]" de ApiControllerBase,
// que además expone UsuarioActualId (id del usuario autenticado del JWT).
public class UsuarioController : ApiControllerBase
{
    private readonly IUsuarioService _service;

    public UsuarioController(IUsuarioService service)
    {
        _service = service;
    }

    // GET api/usuario  → solo administradores
    [Authorize(Roles = Roles.Admin)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _service.GetAll();
        return Ok(data);
    }

    // GET api/usuario/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        // Pertenencia: un usuario solo puede consultar su propia cuenta.
        if (id != UsuarioActualId)
            return Forbid();

        var usuario = await _service.GetById(id);
        if (usuario is null)
            return NotFound(new { mensaje = "Usuario no encontrado." });
        return Ok(usuario);
    }

    // POST api/usuario — público: cualquier persona puede registrarse sin token previo
    // [AllowAnonymous] anula la política global solo para este action.
    // El resto de acciones del controller (GetAll, GetById, Put, Delete) sí requieren JWT.
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] UsuarioRequestDto dto)
    {
        var (success, mensaje, id) = await _service.Registrar(dto);
        if (!success)
            return Conflict(new { mensaje });
        return CreatedAtAction(nameof(GetById), new { id }, new { mensaje, id });
    }

    // PUT api/usuario/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, [FromBody] UsuarioRequestDto dto)
    {
        // Pertenencia: un usuario solo puede editar su propia cuenta.
        if (id != UsuarioActualId)
            return Forbid();

        var (success, mensaje) = await _service.Actualizar(id, dto);
        if (!success)
            return BadRequest(new { mensaje });
        return Ok(new { mensaje });
    }

    // DELETE api/usuario/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        // Pertenencia: un usuario solo puede eliminar su propia cuenta.
        if (id != UsuarioActualId)
            return Forbid();

        var (success, mensaje) = await _service.Eliminar(id);
        if (!success)
            return NotFound(new { mensaje });
        return Ok(new { mensaje });
    }
}

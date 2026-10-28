using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChillDrive_Backend.Controllers;

// Plan Profesional: el conductor elige y paga desde la app; los pagos con dinero (Yape /
// efectivo) quedan pendientes hasta que un administrador los aprueba desde el panel.
public class SuscripcionController : ApiControllerBase
{
    private readonly ISuscripcionService _service;

    public SuscripcionController(ISuscripcionService service)
    {
        _service = service;
    }

    // ── Conductor ────────────────────────────────────────────────────────────

    // GET api/suscripcion/planes  → catálogo con disponibilidad y datos para pagar con Yape
    [HttpGet("planes")]
    public async Task<IActionResult> GetPlanes()
    {
        var data = await _service.GetPlanes(UsuarioActualId);
        return Ok(data);
    }

    // GET api/suscripcion/mi-estado  → plan actual, días restantes, pago pendiente, historial
    [HttpGet("mi-estado")]
    public async Task<IActionResult> GetMiEstado()
    {
        var data = await _service.GetEstado(UsuarioActualId);
        if (data is null)
            return NotFound(new { mensaje = "Usuario no encontrado." });
        return Ok(data);
    }

    // POST api/suscripcion/solicitar  → elegir plan (prueba / puntos / Yape / efectivo)
    // El usuario sale del token, no del body: nadie puede solicitar a nombre de otro.
    [HttpPost("solicitar")]
    public async Task<IActionResult> Solicitar([FromBody] SolicitarSuscripcionRequestDto dto)
    {
        var (success, mensaje, id) = await _service.Solicitar(UsuarioActualId, dto);
        if (!success)
            return BadRequest(new { mensaje });
        return CreatedAtAction(nameof(GetById), new { id }, new { mensaje, id });
    }

    // GET api/suscripcion/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var s = await _service.GetById(id);
        if (s is null)
            return NotFound(new { mensaje = "Suscripción no encontrada." });

        // Pertenencia: solo el dueño (o un administrador) puede ver la suscripción.
        if (s.UsuarioId != UsuarioActualId && !User.IsInRole(Roles.Admin))
            return Forbid();

        return Ok(s);
    }

    // GET api/suscripcion/usuario/5  → historial de suscripciones de un usuario
    [HttpGet("usuario/{usuarioId:int}")]
    public async Task<IActionResult> GetByUsuario(int usuarioId)
    {
        if (usuarioId != UsuarioActualId)
            return Forbid();

        var data = await _service.GetByUsuarioId(usuarioId);
        return Ok(data);
    }

    // GET api/suscripcion/usuario/5/activa  → suscripción vigente
    [HttpGet("usuario/{usuarioId:int}/activa")]
    public async Task<IActionResult> GetActiva(int usuarioId)
    {
        if (usuarioId != UsuarioActualId)
            return Forbid();

        var s = await _service.GetActivaByUsuarioId(usuarioId);
        if (s is null)
            return NotFound(new { mensaje = "El usuario no tiene suscripción activa." });
        return Ok(s);
    }

    // PATCH api/suscripcion/5/cancelar  → retirar un pago que aún está en verificación
    [HttpPatch("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id)
    {
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

    // ── Administrador ────────────────────────────────────────────────────────

    // GET api/suscripcion  → todas (vence primero las que correspondan)
    [Authorize(Roles = Roles.Admin)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _service.GetAll();
        return Ok(data);
    }

    // GET api/suscripcion/pendientes  → pagos por verificar, en orden de llegada
    [Authorize(Roles = Roles.Admin)]
    [HttpGet("pendientes")]
    public async Task<IActionResult> GetPendientes()
    {
        var data = await _service.GetPendientes();
        return Ok(data);
    }

    // PATCH api/suscripcion/5/aprobar  → el pago llegó: activa el plan Profesional
    [Authorize(Roles = Roles.Admin)]
    [HttpPatch("{id:int}/aprobar")]
    public async Task<IActionResult> Aprobar(int id)
    {
        var (success, mensaje) = await _service.Aprobar(id);
        if (!success)
            return BadRequest(new { mensaje });
        return Ok(new { mensaje });
    }

    // PATCH api/suscripcion/5/rechazar  → el pago no se pudo verificar
    [Authorize(Roles = Roles.Admin)]
    [HttpPatch("{id:int}/rechazar")]
    public async Task<IActionResult> Rechazar(int id, [FromBody] RechazarSuscripcionRequestDto dto)
    {
        var (success, mensaje) = await _service.Rechazar(id, dto.Motivo);
        if (!success)
            return BadRequest(new { mensaje });
        return Ok(new { mensaje });
    }
}

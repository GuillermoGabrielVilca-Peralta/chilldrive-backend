using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ChillDrive_Backend.Controllers;

/// <summary>
/// Endpoint público de autenticación.
/// No lleva [Authorize] — es el punto de entrada antes de tener token.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;

    public AuthController(IAuthService service)
    {
        _service = service;
    }

    // POST api/auth/login
    // Recibe correo y contraseña, devuelve el token JWT si las credenciales son válidas.
    // El token debe enviarse en requests posteriores como: Authorization: Bearer {token}
    // [AllowAnonymous] anula la política global para este endpoint — es el único punto
    // de entrada antes de tener token, así que tiene que ser accesible sin autenticación.
    // [EnableRateLimiting] limita los intentos por IP (anti fuerza bruta).
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var (success, mensaje, data) = await _service.Login(dto);
        if (!success)
            return Unauthorized(new { mensaje });
        return Ok(data);
    }
}

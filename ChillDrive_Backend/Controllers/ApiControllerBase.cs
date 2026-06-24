using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace ChillDrive_Backend.Controllers;

/// <summary>
/// Controlador base del que heredan todos los controllers de la API.
/// Centraliza los atributos comunes ([ApiController] y la ruta "api/[controller]")
/// y expone el id del usuario autenticado para validar pertenencia de recursos.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Id del usuario autenticado, extraído del claim NameIdentifier del JWT.
    /// Disponible en cualquier endpoint protegido (todos lo son salvo los [AllowAnonymous]).
    /// Si el token no trae el claim, lanza excepción que el middleware convierte en error controlado.
    /// </summary>
    protected int UsuarioActualId =>
        int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("El token no contiene el id del usuario."));
}

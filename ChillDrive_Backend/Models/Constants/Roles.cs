namespace ChillDrive_Backend.Models.Constants;

/// <summary>
/// Roles de autorización del sistema. El rol viaja como claim dentro del JWT
/// y se usa con [Authorize(Roles = Roles.Admin)] para proteger endpoints administrativos.
/// La pertenencia al rol Admin se define en appsettings.json ("Admin:Correos").
/// </summary>
public static class Roles
{
    public const string Usuario = "Usuario";
    public const string Admin   = "Admin";
}

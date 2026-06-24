namespace ChillDrive_Backend.Models.DTOs.Response;

public class UsuarioResponseDto
{
    public int      Id            { get; set; }
    public string   Nombres       { get; set; } = null!;
    public string   Apellidos     { get; set; } = null!;
    public string   Correo        { get; set; } = null!;
    public string?  Telefono      { get; set; }
    public string?  FotoPerfil    { get; set; }
    public string   TipoPlan      { get; set; } = null!;
    public string   TipoConductor { get; set; } = null!;
    public string   Estado        { get; set; } = null!;
    public DateTime FechaRegistro { get; set; }
    public DateTime? UltimoAcceso { get; set; }
}

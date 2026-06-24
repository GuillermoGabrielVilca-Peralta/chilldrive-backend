namespace ChillDrive_Backend.Models.DTOs.Response;

public class PuntosUsuarioResponseDto
{
    public int      UsuarioId            { get; set; }
    public string   NombreUsuario        { get; set; } = null!;
    public int      PuntosActuales       { get; set; }
    public int      PuntosHistoricos     { get; set; }
    public string   Nivel                { get; set; } = null!;
    public DateTime UltimaActualizacion  { get; set; }
}

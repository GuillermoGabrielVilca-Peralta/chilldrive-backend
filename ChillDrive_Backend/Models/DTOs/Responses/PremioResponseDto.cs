namespace ChillDrive_Backend.Models.DTOs.Response;

public class PremioResponseDto
{
    public int       Id               { get; set; }
    public int       AliadoId         { get; set; }
    public string    NombreAliado     { get; set; } = null!;
    public string    Nombre           { get; set; } = null!;
    public string?   Descripcion      { get; set; }
    public int       PuntosRequeridos { get; set; }
    public int       Stock            { get; set; }
    public DateOnly? FechaInicio      { get; set; }
    public DateOnly? FechaFin         { get; set; }
    public string?   Imagen           { get; set; }
    public string    Estado           { get; set; } = null!;
    public DateTime  FechaRegistro    { get; set; }
}

namespace ChillDrive_Backend.Models.DTOs.Response;

public class CanjeResponseDto
{
    public int       Id               { get; set; }
    public int       UsuarioId        { get; set; }
    public int       PremioId         { get; set; }
    public string    NombrePremio     { get; set; } = null!;
    public string    NombreAliado     { get; set; } = null!;
    public int       PuntosUtilizados { get; set; }
    public string    CodigoQr         { get; set; } = null!;
    public DateTime  FechaCanje       { get; set; }
    public DateTime? FechaUso         { get; set; }
    public string    Estado           { get; set; } = null!;
}

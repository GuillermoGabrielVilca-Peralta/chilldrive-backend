namespace ChillDrive_Backend.Models.DTOs.Response;

public class EventoResponseDto
{
    public int       Id              { get; set; }
    public int?      ViajeId         { get; set; }
    public int       CamaraId        { get; set; }
    public string    TipoEvento      { get; set; } = null!;
    public int       ImpactoPuntaje  { get; set; }
    public decimal?  Latitud         { get; set; }
    public decimal?  Longitud        { get; set; }
    public string?   FotoEvidencia   { get; set; }
    public DateTime  FechaEvento     { get; set; }
}

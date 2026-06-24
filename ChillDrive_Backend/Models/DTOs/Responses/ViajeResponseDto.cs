namespace ChillDrive_Backend.Models.DTOs.Response;

public class ViajeResponseDto
{
    public int       Id                 { get; set; }
    public int       UsuarioId          { get; set; }
    public int       VehiculoId         { get; set; }
    public DateTime  FechaInicio        { get; set; }
    public DateTime? FechaFin           { get; set; }
    public decimal?  DistanciaKm        { get; set; }
    public int?      DuracionMinutos    { get; set; }
    public decimal?  PuntajeConduccion  { get; set; }
    public int       PuntosGanados      { get; set; }
    public string    Estado             { get; set; } = null!;
}
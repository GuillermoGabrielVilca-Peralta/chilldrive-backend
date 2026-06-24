namespace ChillDrive_Backend.Models.DTOs.Response;

public class CamaraResponseDto
{
    public int      Id           { get; set; }
    public int      VehiculoId   { get; set; }
    public string?  Nombre       { get; set; }
    public string?  DireccionIp  { get; set; }
    public string   Posicion     { get; set; } = null!;
    public string   Estado       { get; set; } = null!;
    public DateTime FechaRegistro { get; set; }
}
namespace ChillDrive_Backend.Models.DTOs.Response;

public class VehiculoResponseDto
{
    public int      Id            { get; set; }
    public int      UsuarioId     { get; set; }
    public string   Placa         { get; set; } = null!;
    public string?  Marca         { get; set; }
    public string?  Modelo        { get; set; }
    public int?     Anio          { get; set; }
    public string?  Color         { get; set; }
    public string   TipoVehiculo  { get; set; } = null!;
    public string   Estado        { get; set; } = null!;
    public DateTime FechaRegistro { get; set; }
}
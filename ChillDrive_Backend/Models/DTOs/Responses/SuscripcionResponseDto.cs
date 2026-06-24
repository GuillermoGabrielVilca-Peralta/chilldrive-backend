namespace ChillDrive_Backend.Models.DTOs.Response;

public class SuscripcionResponseDto
{
    public int      Id              { get; set; }
    public int      UsuarioId       { get; set; }
    public string   NombreUsuario   { get; set; } = null!;
    public DateOnly FechaInicio     { get; set; }
    public DateOnly FechaFin        { get; set; }
    public decimal  MontoSoles      { get; set; }
    public string   MetodoPago      { get; set; } = null!;
    public string   Estado          { get; set; } = null!;
    public string?  ReferenciaPago  { get; set; }
    public DateTime FechaRegistro   { get; set; }
}

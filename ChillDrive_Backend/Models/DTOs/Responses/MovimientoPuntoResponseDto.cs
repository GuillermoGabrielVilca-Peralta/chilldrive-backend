namespace ChillDrive_Backend.Models.DTOs.Response;

public class MovimientoPuntoResponseDto
{
    public int      Id               { get; set; }
    public int      UsuarioId        { get; set; }
    public string   NombreUsuario    { get; set; } = null!;
    public string   Tipo             { get; set; } = null!;  // Ganado | Canjeado | Ajuste | Expirado
    public int      Puntos           { get; set; }           // positivo o negativo
    public string?  Descripcion      { get; set; }
    public int?     ViajeId          { get; set; }
    public int?     CanjeId          { get; set; }
    public DateTime FechaMovimiento  { get; set; }
}

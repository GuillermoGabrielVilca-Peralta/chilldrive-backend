namespace ChillDrive_Backend.Models.DTOs.Response;

/// <summary>
/// Fila del reporte de suscripciones vigentes (vista v_suscripciones_activas): quién tiene
/// plan Profesional activo, con sus datos de pago y los días que le restan.
/// </summary>
public class SuscripcionActivaResponseDto
{
    public int       UsuarioId     { get; set; }
    public string    Conductor     { get; set; } = null!;
    public string    Correo        { get; set; } = null!;
    public string    TipoConductor { get; set; } = null!;
    public int       SuscripcionId { get; set; }
    public DateOnly? FechaInicio   { get; set; }
    public DateOnly? FechaFin      { get; set; }
    public decimal   MontoSoles    { get; set; }
    public string    MetodoPago    { get; set; } = null!;
    public int       DiasRestantes { get; set; }
}

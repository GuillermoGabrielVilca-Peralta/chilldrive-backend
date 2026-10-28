namespace ChillDrive_Backend.Models.DTOs.Response;

// Catálogo de planes para la pantalla "Planes" de la app, con los datos para pagar.
public class PlanesSuscripcionResponseDto
{
    public List<PlanSuscripcionResponseDto> Planes { get; set; } = new();
    public string? YapeNumero     { get; set; }
    public string? YapeTitular    { get; set; }
    public int     PuntosPorMes   { get; set; }
    public int     PuntosActuales { get; set; }
}

public class PlanSuscripcionResponseDto
{
    public string  Codigo             { get; set; } = null!;
    public string  Nombre             { get; set; } = null!;
    public decimal PrecioSoles        { get; set; }
    public int     Dias               { get; set; }
    public string  Descripcion        { get; set; } = null!;
    public string? Etiqueta           { get; set; }
    public bool    SePagaConDinero    { get; set; }

    // Si este conductor puede elegirlo ahora (p. ej. la prueba gratis es una sola vez).
    public bool    Disponible         { get; set; }
    public string? MotivoNoDisponible { get; set; }
}

// Estado de la suscripción del conductor: lo que muestra la app en "Mi plan" y en los avisos.
public class EstadoSuscripcionResponseDto
{
    public string                   TipoPlan        { get; set; } = null!;
    public SuscripcionResponseDto?  Activa          { get; set; }
    public int?                     DiasRestantes   { get; set; }
    public bool                     PorVencer       { get; set; }
    public SuscripcionResponseDto?  Pendiente       { get; set; }
    public SuscripcionResponseDto?  UltimaRechazada { get; set; }
    public List<SuscripcionResponseDto> Historial   { get; set; } = new();
}

namespace ChillDrive_Backend.Models.DTOs.Response;

// Análisis de rendimiento de la flota para el panel de administración: el periodo
// pedido (últimos N días) comparado con los N días anteriores.
public class RendimientoResponseDto
{
    public int       Dias     { get; set; }
    public DateTime  Desde    { get; set; }
    public DateTime  Hasta    { get; set; }

    public ResumenRendimientoDto Actual   { get; set; } = null!;
    public ResumenRendimientoDto Anterior { get; set; } = null!;

    public CombustibleEstimadoDto Combustible { get; set; } = null!;

    // Por día (7 y 30 días) o por semana (90 días); solo los tramos con viajes.
    public string                     Agrupacion { get; set; } = null!;
    public List<TendenciaRendimientoDto> Tendencia { get; set; } = new();

    public List<EventoPorTipoDto>   PorTipo       { get; set; } = new();
    public List<EventosPorHoraDto>  PorHora       { get; set; } = new();
    public List<ZonaCriticaDto>     ZonasCriticas { get; set; } = new();
    public List<ConductorRendimientoDto> Conductores { get; set; } = new();
}

public class ResumenRendimientoDto
{
    public int     Viajes                  { get; set; }
    public int     Conductores             { get; set; }
    public double  Km                      { get; set; }
    public double  Horas                   { get; set; }
    public double? PuntajePromedio         { get; set; }   // null sin viajes
    public int     Infracciones            { get; set; }
    public double  InfraccionesPor100Km    { get; set; }
    public double? PorcentajeViajesSeguros { get; set; }   // puntaje ≥ 80
    public int     IndiceEficiencia        { get; set; }
    public double  SobreconsumoPorcentaje  { get; set; }
}

// Todo es ESTIMADO (ver Models/Constants/EficienciaCombustible).
public class CombustibleEstimadoDto
{
    public double KmPorGalon             { get; set; }
    public double PrecioGalon            { get; set; }
    public double GalonesConsumidos      { get; set; }
    public double GalonesDesperdiciados  { get; set; }   // por conducción brusca
    public double SolesDesperdiciados    { get; set; }
    // Ahorro frente al periodo anterior con los mismos km; negativo = se gastó más.
    public double GalonesAhorrados       { get; set; }
    public double SolesAhorrados         { get; set; }
}

public class TendenciaRendimientoDto
{
    public DateOnly Inicio               { get; set; }
    public int      Viajes               { get; set; }
    public double   PuntajePromedio      { get; set; }
    public double   InfraccionesPor100Km { get; set; }
}

public class EventoPorTipoDto
{
    public string Tipo             { get; set; } = null!;
    public bool   EsInfraccion     { get; set; }
    public int    Cantidad         { get; set; }
    public int    CantidadAnterior { get; set; }
}

// Hora local de Perú (0–23).
public class EventosPorHoraDto
{
    public int Hora         { get; set; }
    public int Infracciones { get; set; }
    public int Eventos      { get; set; }
}

// Celda de ~110 m donde se concentran infracciones.
public class ZonaCriticaDto
{
    public decimal Latitud       { get; set; }
    public decimal Longitud      { get; set; }
    public int     Infracciones  { get; set; }
    public string  TipoPrincipal { get; set; } = null!;
}

public class ConductorRendimientoDto
{
    public int     UsuarioId             { get; set; }
    public string  Conductor             { get; set; } = null!;
    public string  Placa                 { get; set; } = null!;
    public int     Viajes                { get; set; }
    public double  Km                    { get; set; }
    public double  PuntajePromedio       { get; set; }
    public double? PuntajeAnterior       { get; set; }   // null si no manejó el periodo anterior
    public int     Infracciones          { get; set; }
    public double  InfraccionesPor100Km  { get; set; }
    public int     IndiceEficiencia      { get; set; }
    public string? InfraccionPrincipal   { get; set; }
    public string? MotivoCapacitacion    { get; set; }   // null = no requiere
}

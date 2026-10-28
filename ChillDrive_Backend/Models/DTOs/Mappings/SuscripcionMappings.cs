using ChillDrive_Backend.Models.Constants;
using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Models.DTOs.Mappings;

public static class SuscripcionMappings
{
    public static SuscripcionResponseDto ToResponse(this Suscripcione s) => new()
    {
        Id              = s.Id,
        UsuarioId       = s.UsuarioId,
        NombreUsuario   = s.Usuario is not null
            ? $"{s.Usuario.Nombres} {s.Usuario.Apellidos}"
            : string.Empty,
        CorreoUsuario   = s.Usuario?.Correo,
        Plan            = s.Plan,
        Dias            = s.Dias,
        FechaInicio     = s.FechaInicio,
        FechaFin        = s.FechaFin,
        MontoSoles      = s.MontoSoles,
        MetodoPago      = s.MetodoPago,
        Estado          = s.Estado,
        ReferenciaPago  = s.ReferenciaPago,
        CodigoPago      = s.CodigoPago,
        PuntosUsados    = s.PuntosUsados,
        FechaAprobacion = s.FechaAprobacion,
        MotivoRechazo   = s.MotivoRechazo,
        FechaRegistro   = s.FechaRegistro
    };

    // `disponible`/`motivo` los decide el servicio según el historial y los puntos del conductor.
    public static PlanSuscripcionResponseDto ToResponse(
        this PlanesSuscripcion.Plan p, bool disponible, string? motivoNoDisponible) => new()
    {
        Codigo             = p.Codigo,
        Nombre             = p.Nombre,
        PrecioSoles        = p.PrecioSoles,
        Dias               = p.Dias,
        Descripcion        = p.Descripcion,
        Etiqueta           = p.Etiqueta,
        SePagaConDinero    = p.SePagaConDinero,
        Disponible         = disponible,
        MotivoNoDisponible = disponible ? null : motivoNoDisponible
    };

    // Nueva suscripción a partir del plan del catálogo: precio y días salen del servidor.
    // Si nace "Pendiente", las fechas son provisionales y se recalculan al aprobarla.
    public static Suscripcione ToEntity(
        this PlanesSuscripcion.Plan plan,
        int      usuarioId,
        string   metodoPago,
        string   estado,
        DateOnly inicio,
        string?  referenciaPago = null,
        string?  codigoPago     = null,
        int?     puntosUsados   = null) => new()
    {
        UsuarioId       = usuarioId,
        Plan            = plan.Codigo,
        Dias            = plan.Dias,
        FechaInicio     = inicio,
        FechaFin        = inicio.AddDays(plan.Dias),
        MontoSoles      = plan.PrecioSoles,
        MetodoPago      = metodoPago,
        Estado          = estado,
        ReferenciaPago  = referenciaPago,
        CodigoPago      = codigoPago,
        PuntosUsados    = puntosUsados,
        FechaAprobacion = estado == PlanesSuscripcion.Activa ? DateTime.UtcNow : null,
        FechaRegistro   = DateTime.UtcNow
    };
}

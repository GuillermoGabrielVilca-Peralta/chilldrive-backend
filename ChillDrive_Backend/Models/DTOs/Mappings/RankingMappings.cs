using ChillDrive_Backend.Models.DTOs.Response;

namespace ChillDrive_Backend.Models.DTOs.Mappings;

public static class RankingMappings
{
    // La vista trae todos los campos nullable (es keyless); el mapper los normaliza.
    public static RankingConductorResponseDto ToResponse(this VRankingConductore v) => new()
    {
        UsuarioId        = v.UsuarioId        ?? 0,
        Conductor        = v.Conductor        ?? string.Empty,
        TipoConductor    = v.TipoConductor    ?? string.Empty,
        PuntosActuales   = v.PuntosActuales   ?? 0,
        PuntosHistoricos = v.PuntosHistoricos ?? 0,
        Nivel            = v.Nivel            ?? string.Empty,
        PuntajePromedio  = v.PuntajePromedio  ?? 0m,
        TotalViajes      = v.TotalViajes      ?? 0
    };
}

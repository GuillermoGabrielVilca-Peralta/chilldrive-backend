namespace ChillDrive_Backend.Models.Constants;

/// <summary>
/// Perú no tiene horario de verano: UTC-5 fijo. Centraliza el cálculo del día
/// calendario local para reglas que dependen de la fecha (vigencia de premios,
/// racha, etc.), de forma consistente en toda la API.
/// </summary>
public static class FechaPeru
{
    public const int OffsetHoras = -5;

    /// <summary>Instante actual en hora de Perú.</summary>
    public static DateTime Ahora => DateTime.UtcNow.AddHours(OffsetHoras);

    /// <summary>Día calendario de hoy en Perú.</summary>
    public static DateOnly Hoy => DateOnly.FromDateTime(Ahora);
}

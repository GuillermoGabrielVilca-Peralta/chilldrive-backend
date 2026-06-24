namespace ChillDrive_Backend.Models.Constants;

/// <summary>
/// Tipos de movimiento de puntos. Deben coincidir con la columna <c>tipo</c> de la tabla
/// <c>movimientos_puntos</c>. Centralizados aquí para evitar "magic strings" dispersas.
/// </summary>
public static class MovimientoTipos
{
    // Valores EXACTOS permitidos por el CHECK constraint chk_movpuntos_tipo de la BD.
    // Regla chk_movpuntos_coherencia: Ganado→puntos>0, Canjeado→puntos<0, Ajuste/Expirado→cualquier signo.
    public const string Ganado   = "Ganado";    // puntos ganados (siempre > 0)
    public const string Canjeado = "Canjeado";  // puntos gastados en un canje (siempre < 0)
    public const string Ajuste   = "Ajuste";    // correcciones: penalizaciones (−) y devoluciones (+)
    public const string Expirado = "Expirado";  // puntos vencidos (reservado, no usado aún)
}

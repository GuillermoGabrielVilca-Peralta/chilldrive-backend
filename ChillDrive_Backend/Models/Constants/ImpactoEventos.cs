namespace ChillDrive_Backend.Models.Constants;

/// <summary>
/// Cuántos puntos suma o resta cada tipo de evento detectado por la IA.
/// El BACKEND es la fuente de verdad de estos valores (no el cliente), igual que en
/// la acreditación manual: así nadie puede enviarse puntos arbitrarios.
/// Positivo = buena conducción (suma) | Negativo = infracción (resta).
/// Ajusta libremente estos valores según las reglas del negocio.
/// </summary>
public static class ImpactoEventos
{
    private static readonly Dictionary<string, int> Valores = new()
    {
        ["Señal Respetada"]  =  10,
        ["Carril Mantenido"] =   5,
        ["Distancia Segura"] =   5,
        ["Peaton Detectado"] =   0,   // alerta informativa, sin impacto en puntos
        ["Señal Ignorada"]   = -20,
        ["Carril Invadido"]  = -15,
        ["Frenado Brusco"]   = -10,
        ["Exceso Velocidad"] = -25,
        ["Otro"]             =   0
    };

    /// <summary>Devuelve el impacto del tipo de evento; 0 si el tipo no tiene puntaje definido.</summary>
    public static int Para(string tipoEvento) => Valores.GetValueOrDefault(tipoEvento, 0);
}

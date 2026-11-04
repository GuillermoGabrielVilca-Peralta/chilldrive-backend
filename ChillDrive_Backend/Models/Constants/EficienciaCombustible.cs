namespace ChillDrive_Backend.Models.Constants;

/// <summary>
/// ESTIMACIÓN del combustible extra que gasta la conducción brusca. ChillDrive no lee el
/// consumo real del vehículo (no hay conexión al motor ni al tanque): parte de los km del
/// viaje y de las maniobras que la IA detecta. El ahorro que muestra el panel es por eso un
/// estimado y así se rotula.
///
/// Referencia: la conducción agresiva (excesos de velocidad, aceleraciones y frenadas
/// bruscas) reduce el rendimiento entre 10 % y 40 % según el tráfico (fueleconomy.gov,
/// Departamento de Energía de EE. UU.). El sobreconsumo crece con las maniobras cada 100 km
/// y se topa en <see cref="SobreconsumoMaximo"/>.
/// </summary>
public static class EficienciaCombustible
{
    /// <summary>Maniobras que aumentan el consumo de combustible.</summary>
    public static readonly string[] TiposQueConsumen =
    [
        "Aceleración Brusca", "Frenado Brusco", "Exceso Velocidad"
    ];

    /// <summary>Sobreconsumo que agrega cada maniobra por cada 100 km (0,5 %).</summary>
    public const double SobreconsumoPorManiobra = 0.005;

    /// <summary>Tope del sobreconsumo estimado (30 %).</summary>
    public const double SobreconsumoMaximo = 0.30;

    // Valores por defecto si el panel no envía los de su flota (auto compacto en ciudad).
    public const double KmPorGalonPorDefecto  = 40;
    public const double PrecioGalonPorDefecto = 16;

    /// <summary>Fracción de combustible extra (0 – 0,30) para tantas maniobras en tantos km.</summary>
    public static double Sobreconsumo(int maniobras, double km)
    {
        if (km <= 0) return 0;
        return Math.Min(SobreconsumoMaximo, maniobras / km * 100 * SobreconsumoPorManiobra);
    }

    /// <summary>Índice 0–100: 100 = sin sobreconsumo, 0 = en el tope.</summary>
    public static int Indice(double sobreconsumo) =>
        (int)Math.Round(100 * (1 - sobreconsumo / SobreconsumoMaximo));
}

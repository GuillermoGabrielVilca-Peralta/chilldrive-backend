namespace ChillDrive_Backend.Services.Implements;

/// <summary>
/// Implementación del servicio de hashing usando BCrypt.
/// WorkFactor 12 = ~300 ms de cómputo por hash, equilibrio entre
/// seguridad y rendimiento aceptable para una API de este tamaño.
/// </summary>
public class PasswordService : IPasswordService
{
    private const int WorkFactor = 12;

    /// <summary>
    /// Genera un hash BCrypt con salt aleatorio incluido.
    /// El salt se almacena dentro del propio string de hash — no hay que guardarlo por separado.
    /// </summary>
    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    /// <summary>
    /// Compara el texto plano contra el hash almacenado.
    /// BCrypt extrae el salt del hash automáticamente para hacer la comparación.
    /// </summary>
    public bool Verificar(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}

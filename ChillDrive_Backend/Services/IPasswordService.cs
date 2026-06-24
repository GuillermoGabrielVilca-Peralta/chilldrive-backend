namespace ChillDrive_Backend.Services;

/// <summary>
/// Contrato para el servicio de hashing de contraseñas.
/// Separado como interfaz para que el resto del código no dependa
/// de BCrypt directamente y se pueda cambiar el algoritmo sin tocar Services.
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Genera el hash BCrypt de una contraseña en texto plano.
    /// Llamar antes de persistir cualquier contraseña en la BD.
    /// </summary>
    string Hash(string password);

    /// <summary>
    /// Compara una contraseña en texto plano contra un hash almacenado.
    /// Devuelve true si coinciden. Usado en el login.
    /// </summary>
    bool Verificar(string password, string hash);
}

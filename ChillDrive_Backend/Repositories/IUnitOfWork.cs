namespace ChillDrive_Backend.Repositories;

/// <summary>
/// Unidad de trabajo: ejecuta una operación que abarca varios SaveChanges dentro de una
/// única transacción de base de datos. Si algo falla, se revierte todo (atomicidad "todo o nada").
/// Como todos los repositorios comparten el mismo DbContext (scoped por request), sus cambios
/// participan automáticamente en la transacción iniciada aquí.
/// </summary>
public interface IUnitOfWork
{
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operacion);
}

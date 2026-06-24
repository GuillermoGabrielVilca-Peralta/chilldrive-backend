using ChillDrive_Backend.Repositories;

namespace ChillDrive_Backend.Tests;

/// <summary>
/// Implementación de IUnitOfWork para tests unitarios: ejecuta la operación directamente,
/// sin transacción real (no hay base de datos en los tests). Permite probar los servicios
/// que envuelven mutaciones en _uow.ExecuteInTransactionAsync sin un DbContext.
/// </summary>
public class FakeUnitOfWork : IUnitOfWork
{
    public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operacion) => operacion();
}

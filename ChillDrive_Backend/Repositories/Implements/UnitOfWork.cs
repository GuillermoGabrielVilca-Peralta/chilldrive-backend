using ChillDrive_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace ChillDrive_Backend.Repositories.Implements;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operacion)
    {
        // Inicia una transacción sobre el DbContext compartido. Los SaveChanges que hagan
        // los repositorios dentro de 'operacion' quedan dentro de esta transacción.
        await using var transaccion = await _context.Database.BeginTransactionAsync();
        try
        {
            var resultado = await operacion();
            await transaccion.CommitAsync();    // confirma todos los cambios juntos
            return resultado;
        }
        catch
        {
            await transaccion.RollbackAsync();  // revierte todo ante cualquier error
            throw;
        }
    }
}

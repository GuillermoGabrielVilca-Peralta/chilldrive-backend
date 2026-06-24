using ChillDrive_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace ChillDrive_Backend.Repositories.Implements;

public class VehiculoRepository : IVehiculoRepository
{
    private readonly ApplicationDbContext _context;

    public VehiculoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Vehiculo>> GetAll()
    {
        return await _context.Vehiculos
            .OrderBy(v => v.Id)
            .ToListAsync();
    }

    public async Task<List<Vehiculo>> GetByUsuarioId(int usuarioId)
    {
        return await _context.Vehiculos
            .Where(v => v.UsuarioId == usuarioId)
            .OrderBy(v => v.Id)
            .ToListAsync();
    }

    public async Task<Vehiculo?> GetById(int id)
    {
        return await _context.Vehiculos.FindAsync(id);
    }

    public async Task<bool> ExistePorPlaca(string placa)
    {
        return await _context.Vehiculos
            .AnyAsync(v => v.Placa == placa);
    }

    public async Task<int> Add(Vehiculo vehiculo)
    {
        await _context.Vehiculos.AddAsync(vehiculo);
        await _context.SaveChangesAsync();
        return vehiculo.Id;
    }

    public async Task Update(Vehiculo vehiculo)
    {
        _context.Vehiculos.Update(vehiculo);
        await _context.SaveChangesAsync();
    }

    public async Task Delete(Vehiculo vehiculo)
    {
        _context.Vehiculos.Remove(vehiculo);
        await _context.SaveChangesAsync();
    }
}

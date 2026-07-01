using ChillDrive_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace ChillDrive_Backend.Repositories.Implements;

public class ViajeRepository : IViajeRepository
{
    private readonly ApplicationDbContext _context;

    public ViajeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Viaje>> GetAll()
    {
        return await _context.Viajes
            .OrderByDescending(v => v.FechaInicio)
            .ToListAsync();
    }

    public async Task<List<Viaje>> GetByUsuarioId(int usuarioId)
    {
        return await _context.Viajes
            .Where(v => v.UsuarioId == usuarioId)
            .OrderByDescending(v => v.FechaInicio)
            .ToListAsync();
    }

    public async Task<(List<Viaje> items, int total)> GetByUsuarioIdPaginado(int usuarioId, int skip, int take)
    {
        var query = _context.Viajes
            .Where(v => v.UsuarioId == usuarioId)
            .OrderByDescending(v => v.FechaInicio);

        var total = await query.CountAsync();
        var items = await query.Skip(skip).Take(take).ToListAsync();
        return (items, total);
    }

    public async Task<List<Viaje>> GetByVehiculoId(int vehiculoId)
    {
        return await _context.Viajes
            .Where(v => v.VehiculoId == vehiculoId)
            .OrderByDescending(v => v.FechaInicio)
            .ToListAsync();
    }

    public async Task<Viaje?> GetById(int id)
    {
        return await _context.Viajes.FindAsync(id);
    }

    // Viajes abiertos ("En Curso") de un usuario, del más reciente al más antiguo.
    // Sirve para garantizar un solo viaje activo: retomar el de hoy y cerrar los viejos.
    public async Task<List<Viaje>> GetEnCursoByUsuario(int usuarioId)
    {
        return await _context.Viajes
            .Where(v => v.UsuarioId == usuarioId && v.Estado == "En Curso")
            .OrderByDescending(v => v.FechaInicio)
            .ToListAsync();
    }

    public async Task<int> Add(Viaje viaje)
    {
        await _context.Viajes.AddAsync(viaje);
        await _context.SaveChangesAsync();
        return viaje.Id;
    }

    public async Task Update(Viaje viaje)
    {
        _context.Viajes.Update(viaje);
        await _context.SaveChangesAsync();
    }

    public async Task Delete(Viaje viaje)
    {
        _context.Viajes.Remove(viaje);
        await _context.SaveChangesAsync();
    }
}
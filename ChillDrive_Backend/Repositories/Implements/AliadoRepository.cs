using ChillDrive_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace ChillDrive_Backend.Repositories.Implements;

public class AliadoRepository : IAliadoRepository
{
    private readonly ApplicationDbContext _context;

    public AliadoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Aliado>> GetAll()
    {
        return await _context.Aliados
            .OrderBy(a => a.Nombre)
            .ToListAsync();
    }

    public async Task<List<Aliado>> GetByCategoria(string categoria)
    {
        return await _context.Aliados
            .Where(a => a.Categoria == categoria)
            .OrderBy(a => a.Nombre)
            .ToListAsync();
    }

    public async Task<Aliado?> GetById(int id)
    {
        return await _context.Aliados.FindAsync(id);
    }

    public async Task<bool> ExistePorRuc(string ruc)
    {
        return await _context.Aliados
            .AnyAsync(a => a.Ruc == ruc);
    }

    public async Task<int> Add(Aliado aliado)
    {
        await _context.Aliados.AddAsync(aliado);
        await _context.SaveChangesAsync();
        return aliado.Id;
    }

    public async Task Update(Aliado aliado)
    {
        _context.Aliados.Update(aliado);
        await _context.SaveChangesAsync();
    }

    public async Task Delete(Aliado aliado)
    {
        _context.Aliados.Remove(aliado);
        await _context.SaveChangesAsync();
    }
}

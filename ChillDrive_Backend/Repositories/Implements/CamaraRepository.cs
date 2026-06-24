using ChillDrive_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace ChillDrive_Backend.Repositories.Implements;

public class CamaraRepository : ICamaraRepository
{
    private readonly ApplicationDbContext _context;

    public CamaraRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Camara>> GetAll()
    {
        return await _context.Camaras
            .OrderBy(c => c.Id)
            .ToListAsync();
    }

    public async Task<List<Camara>> GetByVehiculoId(int vehiculoId)
    {
        return await _context.Camaras
            .Where(c => c.VehiculoId == vehiculoId)
            .OrderBy(c => c.Id)
            .ToListAsync();
    }

    public async Task<Camara?> GetById(int id)
    {
        return await _context.Camaras.FindAsync(id);
    }

    public async Task<int> Add(Camara camara)
    {
        await _context.Camaras.AddAsync(camara);
        await _context.SaveChangesAsync();
        return camara.Id;
    }

    public async Task Update(Camara camara)
    {
        _context.Camaras.Update(camara);
        await _context.SaveChangesAsync();
    }

    public async Task Delete(Camara camara)
    {
        _context.Camaras.Remove(camara);
        await _context.SaveChangesAsync();
    }
}
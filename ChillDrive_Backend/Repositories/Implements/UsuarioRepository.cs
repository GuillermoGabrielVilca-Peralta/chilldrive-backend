using ChillDrive_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace ChillDrive_Backend.Repositories.Implements;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly ApplicationDbContext _context;

    public UsuarioRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Usuario>> GetAll()
    {
        return await _context.Usuarios
            .OrderBy(u => u.Id)
            .ToListAsync();
    }

    public async Task<Usuario?> GetById(int id)
    {
        return await _context.Usuarios.FindAsync(id);
    }

    public async Task<Usuario?> GetByCorreo(string correo)
    {
        return await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Correo == correo);
    }

    public async Task<bool> ExistePorCorreo(string correo)
    {
        return await _context.Usuarios
            .AnyAsync(u => u.Correo == correo);
    }

    public async Task<int> Add(Usuario usuario)
    {
        await _context.Usuarios.AddAsync(usuario);
        await _context.SaveChangesAsync();
        return usuario.Id;
    }

    public async Task Update(Usuario usuario)
    {
        _context.Usuarios.Update(usuario);
        await _context.SaveChangesAsync();
    }

    public async Task Delete(Usuario usuario)
    {
        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
    }
}


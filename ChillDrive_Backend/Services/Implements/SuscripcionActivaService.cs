using ChillDrive_Backend.Models.DTOs.Mappings;
using ChillDrive_Backend.Models.DTOs.Response;
using ChillDrive_Backend.Repositories;

namespace ChillDrive_Backend.Services.Implements;

public class SuscripcionActivaService : ISuscripcionActivaService
{
    private readonly ISuscripcionActivaRepository _repo;

    public SuscripcionActivaService(ISuscripcionActivaRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<SuscripcionActivaResponseDto>> GetActivas()
    {
        var lista = await _repo.GetActivas();
        return lista.Select(v => v.ToResponse()).ToList();
    }
}

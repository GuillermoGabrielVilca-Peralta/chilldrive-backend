using ChillDrive_Backend.Models.DTOs.Mappings;
using ChillDrive_Backend.Models.DTOs.Requests;
using ChillDrive_Backend.Models.DTOs.Response;
using ChillDrive_Backend.Repositories;

namespace ChillDrive_Backend.Services.Implements;

public class SuscripcionService : ISuscripcionService
{
    private readonly ISuscripcionRepository _repo;
    private readonly IUsuarioRepository     _usuarioRepo;
    private readonly IUnitOfWork            _uow;

    private static readonly string[] MetodosPagoValidos =
        ["Yape", "Plin", "Tarjeta", "Efectivo", "Otro"];

    public SuscripcionService(
        ISuscripcionRepository repo,
        IUsuarioRepository     usuarioRepo,
        IUnitOfWork            uow)
    {
        _repo        = repo;
        _usuarioRepo = usuarioRepo;
        _uow         = uow;
    }

    public async Task<List<SuscripcionResponseDto>> GetAll()
    {
        var lista = await _repo.GetAll();
        return lista.Select(s => s.ToResponse()).ToList();
    }

    public async Task<List<SuscripcionResponseDto>> GetByUsuarioId(int usuarioId)
    {
        var lista = await _repo.GetByUsuarioId(usuarioId);
        return lista.Select(s => s.ToResponse()).ToList();
    }

    public async Task<SuscripcionResponseDto?> GetById(int id)
    {
        var s = await _repo.GetById(id);
        return s?.ToResponse();
    }

    public async Task<SuscripcionResponseDto?> GetActivaByUsuarioId(int usuarioId)
    {
        var s = await _repo.GetActivaByUsuarioId(usuarioId);
        return s?.ToResponse();
    }

    // -------------------------------------------------------------------------
    // REGISTRAR — activa el plan Profesional del usuario
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje, int? id)> Registrar(SuscripcionRequestDto dto)
    {
        var usuario = await _usuarioRepo.GetById(dto.UsuarioId);
        if (usuario is null)
            return (false, "El usuario no existe.", null);

        if (!MetodosPagoValidos.Contains(dto.MetodoPago))
            return (false, $"Método de pago inválido. Valores permitidos: {string.Join(", ", MetodosPagoValidos)}", null);

        if (dto.MontoSoles <= 0)
            return (false, "El monto debe ser mayor a 0.", null);

        if (dto.FechaFin <= dto.FechaInicio)
            return (false, "La fecha de fin debe ser posterior a la fecha de inicio.", null);

        // Verificar si ya tiene suscripción activa
        var activa = await _repo.GetActivaByUsuarioId(dto.UsuarioId);
        if (activa is not null)
            return (false, $"El usuario ya tiene una suscripción activa hasta el {activa.FechaFin}.", null);

        // Crear la suscripción y elevar el plan del usuario, en una sola transacción.
        return await _uow.ExecuteInTransactionAsync<(bool success, string mensaje, int? id)>(async () =>
        {
            var id = await _repo.Add(dto.ToEntity());

            // Actualizar el plan del usuario a Profesional
            usuario.TipoPlan = "Profesional";
            await _usuarioRepo.Update(usuario);

            return (true, "Suscripción registrada. Plan actualizado a Profesional.", id);
        });
    }

    // -------------------------------------------------------------------------
    // CANCELAR — cancela la suscripción y revierte el plan a Gratuito
    // -------------------------------------------------------------------------
    public async Task<(bool success, string mensaje)> Cancelar(int id)
    {
        var suscripcion = await _repo.GetById(id);
        if (suscripcion is null)
            return (false, "Suscripción no encontrada.");

        if (suscripcion.Estado != "Activa")
            return (false, $"Solo se pueden cancelar suscripciones activas. Estado actual: '{suscripcion.Estado}'.");

        // Cancelar la suscripción y revertir el plan del usuario, en una sola transacción.
        return await _uow.ExecuteInTransactionAsync<(bool success, string mensaje)>(async () =>
        {
            suscripcion.Estado = "Cancelada";
            await _repo.Update(suscripcion);

            // Revertir plan del usuario a Gratuito
            var usuario = await _usuarioRepo.GetById(suscripcion.UsuarioId);
            if (usuario is not null)
            {
                usuario.TipoPlan = "Gratuito";
                await _usuarioRepo.Update(usuario);
            }

            return (true, "Suscripción cancelada. Plan revertido a Gratuito.");
        });
    }
}

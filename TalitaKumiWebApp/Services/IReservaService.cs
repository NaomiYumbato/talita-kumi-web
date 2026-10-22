using TalitaKumiWebApp.Models;

namespace TalitaKumiWebApp.Services;

public interface IReservaService
{
    Task<Reserva> CrearReservaAsync(Reserva reserva);
    Task<List<Reserva>> ObtenerReservasAsync();
    Task<Reserva?> ObtenerPorIdAsync(int id);

    Task<List<Reserva>> ObtenerReservasSemanaAsync(DateOnly inicioSemana, DateOnly finSemana);
    Task<List<Reserva>> ObtenerReservasFiltradasAsync(DateOnly? fecha, string? busqueda, string? estado);
    Task<bool> ActualizarEstadoAsync(int id, string nuevoEstado);
    Task<bool> ActualizarNotasAsync(int id, string notas);
    Task<Reserva> CrearReservaManualAsync(TalitaKumiWebApp.ViewModels.Admin.CrearReservaManualViewModel model);
}

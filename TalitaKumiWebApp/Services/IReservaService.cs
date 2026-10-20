using TalitaKumiWebApp.Models;

namespace TalitaKumiWebApp.Services;

public interface IReservaService
{
    Task<Reserva> CrearReservaAsync(Reserva reserva);
    Task<List<Reserva>> ObtenerReservasAsync();
    Task<Reserva?> ObtenerPorIdAsync(int id);
}

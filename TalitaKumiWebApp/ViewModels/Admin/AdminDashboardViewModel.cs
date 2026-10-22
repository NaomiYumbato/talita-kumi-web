using TalitaKumiWebApp.Models;

namespace TalitaKumiWebApp.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public DateOnly FechaSeleccionada { get; set; }
    public DateOnly InicioSemana { get; set; }
    public DateOnly FinSemana { get; set; }

    public List<Reserva> ReservasSemana { get; set; } = new();
    public List<Reserva> ReservasTabla { get; set; } = new();
    public List<Servicio> ServiciosDisponibles { get; set; } = new();

    public string? FiltroBusqueda { get; set; }
    public string? FiltroEstado { get; set; }
}

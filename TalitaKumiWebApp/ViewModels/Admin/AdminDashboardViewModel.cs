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

    /// <summary>
    /// Invoca la verificación de turno directamente desde la vista mediante Model.PerteneceATurno(r.Horario, turnoClave).
    /// </summary>
    public bool PerteneceATurno(string? horario, string turnoClave) => CorrespondeATurno(horario, turnoClave);

    /// <summary>
    /// Determina la clave de turno ("Mañana", "Tarde 1", "Tarde 2") para una cita desde la vista mediante Model.DeterminarTurno(r.Horario).
    /// </summary>
    public string? DeterminarTurno(string? horario) => DeterminarTurnoClave(horario);

    /// <summary>
    /// Determina de forma robusta si el horario de una cita corresponde a un turno clave del calendario ("Mañana", "Tarde 1", "Tarde 2").
    /// Tolera variaciones históricas como "Tarde Temprano", "Tarde Tarde", "Tarde 1", "Tarde 2", y horas puntuales.
    /// </summary>
    public static bool CorrespondeATurno(string? horario, string turnoClave)
    {
        if (string.IsNullOrWhiteSpace(horario)) return false;
        var h = horario.Trim();

        bool esManana = h.Contains("Mañana", StringComparison.OrdinalIgnoreCase);
        bool esTarde2 = h.Contains("Tarde 2", StringComparison.OrdinalIgnoreCase) ||
                        h.Contains("Tarde Tarde", StringComparison.OrdinalIgnoreCase) ||
                        h.Contains("Noche", StringComparison.OrdinalIgnoreCase);
        bool esTarde1 = h.Contains("Tarde 1", StringComparison.OrdinalIgnoreCase) ||
                        h.Contains("Temprano", StringComparison.OrdinalIgnoreCase);

        // Si no coincide con las palabras clave exactas, analizar por hora o contexto
        if (!esManana && !esTarde1 && !esTarde2)
        {
            if (h.Contains("Tarde", StringComparison.OrdinalIgnoreCase))
            {
                if (h.Contains("17:") || h.Contains("18:") || h.Contains("19:") || h.Contains("20:"))
                    esTarde2 = true;
                else
                    esTarde1 = true;
            }
            else if (h.Contains("10:") || h.Contains("11:") || h.Contains("12:"))
            {
                esManana = true;
            }
            else if (h.Contains("13:") || h.Contains("14:") || h.Contains("15:") || h.Contains("16:00"))
            {
                esTarde1 = true;
            }
            else if (h.Contains("16:30") || h.Contains("17:") || h.Contains("18:") || h.Contains("19:"))
            {
                esTarde2 = true;
            }
        }

        return turnoClave switch
        {
            "Mañana" => esManana,
            "Tarde 1" => esTarde1,
            "Tarde 2" => esTarde2,
            _ => h.Contains(turnoClave, StringComparison.OrdinalIgnoreCase)
        };
    }

    /// <summary>
    /// Identifica a cuál de los 3 turnos estándar pertenece un texto de horario ("Mañana", "Tarde 1", "Tarde 2") o null si no se puede clasificar.
    /// </summary>
    public static string? DeterminarTurnoClave(string? horario)
    {
        if (CorrespondeATurno(horario, "Mañana")) return "Mañana";
        if (CorrespondeATurno(horario, "Tarde 1")) return "Tarde 1";
        if (CorrespondeATurno(horario, "Tarde 2")) return "Tarde 2";
        return null;
    }
}

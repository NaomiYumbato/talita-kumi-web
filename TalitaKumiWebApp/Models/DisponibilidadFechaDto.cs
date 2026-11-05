namespace TalitaKumiWebApp.Models;

public class DisponibilidadFechaDto
{
    public DateOnly Fecha { get; set; }
    public List<string> TurnosDisponibles { get; set; } = new();
    public List<string> TurnosOcupados { get; set; } = new();
    public bool EstaCompletamenteOcupado => TurnosDisponibles.Count == 0;
}

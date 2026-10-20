using Microsoft.EntityFrameworkCore;
using TalitaKumiWebApp.Data;
using TalitaKumiWebApp.Models;

namespace TalitaKumiWebApp.Services;

public class ReservaService : IReservaService
{
    private readonly AppDbContext _context;

    public ReservaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Reserva> CrearReservaAsync(Reserva reserva)
    {
        ArgumentNullException.ThrowIfNull(reserva);

        if (string.IsNullOrWhiteSpace(reserva.Nombre))
            throw new ArgumentException("El nombre es requerido.", nameof(reserva.Nombre));

        if (string.IsNullOrWhiteSpace(reserva.Telefono))
            throw new ArgumentException("El teléfono es requerido.", nameof(reserva.Telefono));

        if (string.IsNullOrWhiteSpace(reserva.Servicio))
            throw new ArgumentException("El servicio es requerido.", nameof(reserva.Servicio));

        if (reserva.Fecha < DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("La fecha no puede ser anterior a hoy.", nameof(reserva.Fecha));

        // 1. Vincular o registrar Cliente por teléfono
        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Telefono == reserva.Telefono);
        if (cliente == null)
        {
            cliente = new Cliente
            {
                Nombre = reserva.Nombre,
                Telefono = reserva.Telefono
            };
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();
        }
        else if (!string.IsNullOrWhiteSpace(reserva.Nombre) && cliente.Nombre != reserva.Nombre)
        {
            cliente.Nombre = reserva.Nombre;
        }

        reserva.ClienteId = cliente.Id;

        // 2. Vincular Servicio del catálogo si coincide
        var servicio = await _context.Servicios
            .FirstOrDefaultAsync(s => s.Nombre == reserva.Servicio || reserva.Servicio.StartsWith(s.Nombre));
        if (servicio != null)
        {
            reserva.ServicioId = servicio.Id;
        }

        reserva.Estado = "Pendiente";
        reserva.FechaCreacion = DateTime.UtcNow;

        _context.Reservas.Add(reserva);
        await _context.SaveChangesAsync();

        return reserva;
    }

    public async Task<List<Reserva>> ObtenerReservasAsync()
    {
        return await _context.Reservas
            .Include(r => r.Cliente)
            .Include(r => r.ServicioRelacionado)
            .Include(r => r.CotizacionUña)
            .OrderByDescending(r => r.Fecha)
            .ThenByDescending(r => r.FechaCreacion)
            .ToListAsync();
    }

    public async Task<Reserva?> ObtenerPorIdAsync(int id)
    {
        return await _context.Reservas
            .Include(r => r.Cliente)
            .Include(r => r.ServicioRelacionado)
            .Include(r => r.CotizacionUña)
            .FirstOrDefaultAsync(r => r.Id == id);
    }
}

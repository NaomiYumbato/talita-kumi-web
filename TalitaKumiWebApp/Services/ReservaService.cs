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

        // 2. Vincular Servicio del catálogo si coincide (por Id o por coincidencia inteligente de nombre)
        Servicio? servicio = null;
        if (reserva.ServicioId.HasValue && reserva.ServicioId.Value > 0)
        {
            servicio = await _context.Servicios.FindAsync(reserva.ServicioId.Value);
        }

        if (servicio == null && !string.IsNullOrWhiteSpace(reserva.Servicio))
        {
            var nombre = reserva.Servicio.Trim();
            servicio = await _context.Servicios
                .FirstOrDefaultAsync(s => s.Nombre == nombre || nombre.StartsWith(s.Nombre));

            if (servicio == null)
            {
                if (nombre.Contains("Soft Gel", StringComparison.OrdinalIgnoreCase))
                    servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Nombre == "Soft Gel Tips");
                else if (nombre.Contains("Kapping", StringComparison.OrdinalIgnoreCase))
                    servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Nombre == "Kapping Gel");
                else if (nombre.Contains("Esculpid", StringComparison.OrdinalIgnoreCase))
                    servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Nombre == "Uñas Esculpidas");
                else if (nombre.Contains("Semipermanente", StringComparison.OrdinalIgnoreCase))
                    servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Nombre == "Manicura Semipermanente");
                else if (nombre.Contains("Lifting", StringComparison.OrdinalIgnoreCase))
                    servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Nombre == "Lifting de Pestañas");
                else if (nombre.Contains("Laminado", StringComparison.OrdinalIgnoreCase))
                    servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Nombre == "Laminado de Cejas");
                else if (nombre.Contains("Pedicura Completa", StringComparison.OrdinalIgnoreCase))
                    servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Nombre == "Pedicura Completa con Semipermanente");
                else if (nombre.Contains("Pedicura", StringComparison.OrdinalIgnoreCase))
                    servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Nombre == "Pedicura Spa");
                else if (nombre.Contains("Depilaci", StringComparison.OrdinalIgnoreCase))
                    servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Nombre == "Depilación Facial con Cera");
                else if (nombre.Contains("Retiro", StringComparison.OrdinalIgnoreCase))
                    servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Nombre == "Retiro de Esmalte o Uñas");
            }
        }

        if (servicio != null)
        {
            reserva.ServicioId = servicio.Id;
            if (string.IsNullOrWhiteSpace(reserva.Servicio))
            {
                reserva.Servicio = servicio.Nombre;
            }
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

    public async Task<List<Reserva>> ObtenerReservasSemanaAsync(DateOnly inicioSemana, DateOnly finSemana)
    {
        return await _context.Reservas
            .Include(r => r.Cliente)
            .Include(r => r.ServicioRelacionado)
            .Where(r => r.Fecha >= inicioSemana && r.Fecha <= finSemana)
            .OrderBy(r => r.Fecha)
            .ThenBy(r => r.Horario)
            .ToListAsync();
    }

    public async Task<List<Reserva>> ObtenerReservasFiltradasAsync(DateOnly? fecha, string? busqueda, string? estado)
    {
        var query = _context.Reservas
            .Include(r => r.Cliente)
            .Include(r => r.ServicioRelacionado)
            .AsQueryable();

        if (fecha.HasValue)
        {
            query = query.Where(r => r.Fecha == fecha.Value);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var term = busqueda.Trim().ToLower();
            query = query.Where(r => r.Nombre.ToLower().Contains(term) || r.Telefono.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(estado) && !estado.Equals("Todas", StringComparison.OrdinalIgnoreCase))
        {
            var estadoNorm = NormalizarEstado(estado);
            query = query.Where(r => r.Estado == estadoNorm);
        }

        return await query
            .OrderByDescending(r => r.Fecha)
            .ThenByDescending(r => r.FechaCreacion)
            .ToListAsync();
    }

    public async Task<bool> ActualizarEstadoAsync(int id, string nuevoEstado)
    {
        var reserva = await _context.Reservas.FindAsync(id);
        if (reserva == null) return false;

        var estadoNormalizado = NormalizarEstado(nuevoEstado);
        reserva.Estado = estadoNormalizado;

        if ((estadoNormalizado == "Confirmada" || estadoNormalizado == "Completada") && reserva.MontoAbonado == 0m)
        {
            reserva.MontoAbonado = 20.00m;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActualizarNotasAsync(int id, string notas)
    {
        var reserva = await _context.Reservas.FindAsync(id);
        if (reserva == null) return false;

        var texto = notas?.Trim() ?? string.Empty;
        if (texto.Length > 500)
        {
            texto = texto.Substring(0, 500);
        }

        reserva.Notas = string.IsNullOrWhiteSpace(texto) ? null : texto;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Reserva> CrearReservaManualAsync(TalitaKumiWebApp.ViewModels.Admin.CrearReservaManualViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var telefonoLimpio = model.Telefono.Trim();
        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Telefono == telefonoLimpio);
        if (cliente == null)
        {
            cliente = new Cliente
            {
                Nombre = model.Nombre.Trim(),
                Telefono = telefonoLimpio
            };
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();
        }

        var servicio = await _context.Servicios.FindAsync(model.ServicioId);
        var nombreServicio = servicio?.Nombre ?? "Servicio General";
        var precioFinal = model.Precio ?? servicio?.PrecioBase ?? 0m;
        var duracion = servicio?.DuracionBaseMinutos ?? 60;
        var estadoFinal = NormalizarEstado(model.EstadoInicial);
        var abonoFinal = model.MontoAbonado ?? (estadoFinal == "Confirmada" ? 20.00m : 0.00m);

        var reserva = new Reserva
        {
            ClienteId = cliente.Id,
            ServicioId = servicio?.Id,
            Nombre = model.Nombre.Trim(),
            Telefono = telefonoLimpio,
            Servicio = nombreServicio,
            PrecioEstimado = precioFinal,
            MontoAbonado = abonoFinal,
            DuracionMinutos = duracion,
            Fecha = model.Fecha,
            Horario = model.Horario.Trim(),
            Notas = string.IsNullOrWhiteSpace(model.Notas) ? null : model.Notas.Trim(),
            Estado = estadoFinal,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Reservas.Add(reserva);
        await _context.SaveChangesAsync();
        return reserva;
    }

    private static string NormalizarEstado(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Pendiente";
        var lower = raw.Trim().ToLower();

        if (lower.Contains("confirmad")) return "Confirmada";
        if (lower.Contains("finaliz") || lower.Contains("completad") || lower.Contains("atendid")) return "Completada";
        if (lower.Contains("cancelad")) return "Cancelada";
        return "Pendiente";
    }

    public async Task<List<Servicio>> ObtenerServiciosActivosAsync()
    {
        return await _context.Servicios
            .Where(s => s.Activo)
            .OrderBy(s => s.Categoria)
            .ThenBy(s => s.Nombre)
            .ToListAsync();
    }
}

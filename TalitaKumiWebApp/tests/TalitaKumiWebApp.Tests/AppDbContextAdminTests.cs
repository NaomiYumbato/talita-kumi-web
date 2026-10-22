using Microsoft.EntityFrameworkCore;
using TalitaKumiWebApp.Data;
using TalitaKumiWebApp.Models;
using Xunit;

namespace TalitaKumiWebApp.Tests;

public class AppDbContextAdminTests
{
    [Fact]
    public async Task AppDbContext_PuedeGuardarYRecuperarUsuarioAdmin_YReservaConMontoAbonado()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var admin = new UsuarioAdmin
        {
            Username = "naomi",
            PasswordHash = "AQAAAAEAACcQAAAAEHashTest123",
            NombreCompleto = "Naomi Yumbato",
            Activo = true
        };
        context.UsuariosAdmin.Add(admin);

        var reserva = new Reserva
        {
            Nombre = "Camila Ramos",
            Telefono = "998877665",
            Servicio = "Balayage Personalizado",
            Fecha = new DateOnly(2026, 10, 24),
            Horario = "Mañana (10:00 a 13:00)",
            MontoAbonado = 20.00m
        };
        context.Reservas.Add(reserva);
        await context.SaveChangesAsync();

        var adminDb = await context.UsuariosAdmin.FirstOrDefaultAsync(u => u.Username == "naomi");
        Assert.NotNull(adminDb);
        Assert.Equal("Naomi Yumbato", adminDb.NombreCompleto);
        Assert.True(adminDb.Activo);

        var reservaDb = await context.Reservas.FirstOrDefaultAsync(r => r.Telefono == "998877665");
        Assert.NotNull(reservaDb);
        Assert.Equal(20.00m, reservaDb.MontoAbonado);
    }
}

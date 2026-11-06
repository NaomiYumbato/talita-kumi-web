using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TalitaKumiWebApp.Services;

namespace TalitaKumiWebApp.Data;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabaseSchemaSynchronizer(this IServiceCollection services)
    {
        services.AddScoped<IDatabaseSchemaSynchronizer, DatabaseSchemaSynchronizer>();
        return services;
    }

    public static async Task EnsureDatabaseSynchronizedAsync(this IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var synchronizer = scope.ServiceProvider.GetRequiredService<IDatabaseSchemaSynchronizer>();

        // 1. Sincronizar dinámicamente tablas, columnas e índices según el modelo actual
        await synchronizer.SynchronizeAsync(context);

        // 2. Asegurar que exista el usuario administrador con las credenciales configuradas
        var adminAuth = scope.ServiceProvider.GetRequiredService<IAdminAuthService>();
        var defaultAdminUser = Environment.GetEnvironmentVariable("ADMIN_USERNAME")
            ?? configuration["AdminSettings:DefaultUsername"];

        var defaultAdminPass = Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
            ?? configuration["AdminSettings:DefaultPassword"];

        var defaultAdminName = Environment.GetEnvironmentVariable("ADMIN_NOMBRE")
            ?? configuration["AdminSettings:DefaultNombre"];

        if (!string.IsNullOrWhiteSpace(defaultAdminUser) && !string.IsNullOrWhiteSpace(defaultAdminPass))
        {
            await adminAuth.EnsureDefaultAdminUserAsync(
                defaultAdminUser,
                defaultAdminPass,
                string.IsNullOrWhiteSpace(defaultAdminName) ? defaultAdminUser : defaultAdminName);
        }
    }
}

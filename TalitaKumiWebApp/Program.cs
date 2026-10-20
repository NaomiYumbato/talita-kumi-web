using Microsoft.EntityFrameworkCore;

// 1. Cargar variables de entorno desde .env si existe
var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envFile))
{
    foreach (var line in File.ReadAllLines(envFile))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#')) continue;
        var parts = trimmed.Split('=', 2);
        if (parts.Length == 2)
        {
            Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

var dbServer = Environment.GetEnvironmentVariable("DB_SERVER") ?? "localhost,1433";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "TalitaKumiDb";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "sa";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "sql";

var defaultConn = $"Server={dbServer};Database={dbName};User Id={dbUser};Password={dbPassword};TrustServerCertificate=True;MultipleActiveResultSets=True;";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? defaultConn;

// Si en appsettings o .env la contraseña está configurada, asegurar que use la de .env si existe
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DB_PASSWORD")))
{
    connectionString = defaultConn;
}

builder.Services.AddDbContext<TalitaKumiWebApp.Data.AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    }));

builder.Services.AddSingleton<TalitaKumiWebApp.Services.INailPricingEngine, TalitaKumiWebApp.Services.NailPricingEngine>();
builder.Services.AddScoped<TalitaKumiWebApp.Services.IReservaService, TalitaKumiWebApp.Services.ReservaService>();

var app = builder.Build();

// Auto-crear base de datos y tablas en SQL Server (Docker) si no existen
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TalitaKumiWebApp.Data.AppDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.UseStaticFiles();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

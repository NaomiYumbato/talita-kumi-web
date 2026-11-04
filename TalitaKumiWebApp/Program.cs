using Microsoft.EntityFrameworkCore;
using TalitaKumiWebApp.Data;

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
            var key = parts[0].Trim();
            var val = parts[1].Trim();
            if ((val.StartsWith('"') && val.EndsWith('"')) || (val.StartsWith('\'') && val.EndsWith('\'')))
            {
                val = val.Substring(1, val.Length - 2);
            }
            Environment.SetEnvironmentVariable(key, val);
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var studioSettings = new TalitaKumiWebApp.Models.StudioSettings();
builder.Configuration.GetSection("StudioSettings").Bind(studioSettings);

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STUDIO_WHATSAPP")))
    studioSettings.WhatsAppNumber = Environment.GetEnvironmentVariable("STUDIO_WHATSAPP")!;
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STUDIO_INSTAGRAM")))
    studioSettings.InstagramHandle = Environment.GetEnvironmentVariable("STUDIO_INSTAGRAM")!;
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STUDIO_TIKTOK")))
    studioSettings.TikTokHandle = Environment.GetEnvironmentVariable("STUDIO_TIKTOK")!;

builder.Services.AddSingleton(studioSettings);

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
builder.Services.AddScoped<TalitaKumiWebApp.Services.IAdminAuthService, TalitaKumiWebApp.Services.AdminAuthService>();
builder.Services.AddDatabaseSchemaSynchronizer();

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.LogoutPath = "/Admin/Logout";
        options.AccessDeniedPath = "/Admin/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

var app = builder.Build();

// Sincronizar dinámicamente el esquema de base de datos (tablas, columnas, índices faltantes) y sembrar datos iniciales
await app.Services.EnsureDatabaseSynchronizedAsync(app.Configuration);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();
app.MapStaticAssets();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

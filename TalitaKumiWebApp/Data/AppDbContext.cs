using Microsoft.EntityFrameworkCore;
using TalitaKumiWebApp.Models;
using TalitaKumiWebApp.Models.Cotizaciones;

namespace TalitaKumiWebApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<CotizacionUñaRegistro> CotizacionesUñas => Set<CotizacionUñaRegistro>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Clientes
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Telefono).IsRequired().HasMaxLength(30);
            entity.HasIndex(c => c.Telefono);
        });

        // Servicios
        modelBuilder.Entity<Servicio>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(s => s.Categoria).IsRequired().HasMaxLength(50);
            entity.Property(s => s.PrecioBase).HasColumnType("decimal(10,2)");
        });

        // Reservas
        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.PrecioEstimado).HasColumnType("decimal(10,2)");
            entity.Property(r => r.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(r => r.Telefono).IsRequired().HasMaxLength(30);
            entity.Property(r => r.Servicio).IsRequired().HasMaxLength(150);
            entity.Property(r => r.Horario).IsRequired().HasMaxLength(50);

            entity.HasOne(r => r.Cliente)
                  .WithMany(c => c.Reservas)
                  .HasForeignKey(r => r.ClienteId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(r => r.ServicioRelacionado)
                  .WithMany()
                  .HasForeignKey(r => r.ServicioId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(r => r.CotizacionUña)
                  .WithOne(c => c.Reserva)
                  .HasForeignKey<CotizacionUñaRegistro>(c => c.ReservaId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // CotizacionesUñas
        modelBuilder.Entity<CotizacionUñaRegistro>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.SubtotalTecnica).HasColumnType("decimal(10,2)");
            entity.Property(c => c.SubtotalLargo).HasColumnType("decimal(10,2)");
            entity.Property(c => c.SubtotalArte).HasColumnType("decimal(10,2)");
            entity.Property(c => c.SubtotalRetiro).HasColumnType("decimal(10,2)");
            entity.Property(c => c.PrecioTotalCalculado).HasColumnType("decimal(10,2)");
        });

        // Catálogo de Servicios Iniciales con precios en Soles (S/.)
        modelBuilder.Entity<Servicio>().HasData(
            new Servicio { Id = 1, Categoria = "Uñas", Nombre = "Manicura Semipermanente", Descripcion = "Manicura combinada / rusa con esmaltado semipermanente", PrecioBase = 45m, DuracionBaseMinutos = 60, EsPersonalizableConBot = true, Activo = true },
            new Servicio { Id = 2, Categoria = "Uñas", Nombre = "Kapping Gel", Descripcion = "Refuerzo y nivelación sobre uña natural con gel o acrigel", PrecioBase = 65m, DuracionBaseMinutos = 75, EsPersonalizableConBot = true, Activo = true },
            new Servicio { Id = 3, Categoria = "Uñas", Nombre = "Soft Gel Tips", Descripcion = "Extensiones anatómicas completas de soft gel", PrecioBase = 90m, DuracionBaseMinutos = 90, EsPersonalizableConBot = true, Activo = true },
            new Servicio { Id = 4, Categoria = "Uñas", Nombre = "Uñas Esculpidas", Descripcion = "Esculpidas en acrílico con molde personalizado", PrecioBase = 120m, DuracionBaseMinutos = 120, EsPersonalizableConBot = true, Activo = true },
            new Servicio { Id = 5, Categoria = "Pedicura", Nombre = "Pedicura Spa", Descripcion = "Exfoliación, retiro de durezas y belleza de pies", PrecioBase = 45m, DuracionBaseMinutos = 45, EsPersonalizableConBot = false, Activo = true },
            new Servicio { Id = 6, Categoria = "Pedicura", Nombre = "Pedicura Completa con Semipermanente", Descripcion = "Spa de pies profundo más esmaltado semipermanente", PrecioBase = 65m, DuracionBaseMinutos = 60, EsPersonalizableConBot = false, Activo = true },
            new Servicio { Id = 7, Categoria = "Mirada", Nombre = "Lifting de Pestañas", Descripcion = "Curvatura, nutrición y tinte de pestañas naturales", PrecioBase = 70m, DuracionBaseMinutos = 60, EsPersonalizableConBot = false, Activo = true },
            new Servicio { Id = 8, Categoria = "Mirada", Nombre = "Laminado de Cejas", Descripcion = "Diseño, visagismo y laminado fijador de cejas", PrecioBase = 55m, DuracionBaseMinutos = 45, EsPersonalizableConBot = false, Activo = true },
            new Servicio { Id = 9, Categoria = "Mirada", Nombre = "Combo Lifting + Laminado", Descripcion = "Mirada completa: lifting de pestañas y perfilado/laminado", PrecioBase = 110m, DuracionBaseMinutos = 90, EsPersonalizableConBot = false, Activo = true },
            new Servicio { Id = 10, Categoria = "Otros", Nombre = "Depilación Facial con Cera", Descripcion = "Depilación delicada de bozo o cejas con cera elástica", PrecioBase = 25m, DuracionBaseMinutos = 20, EsPersonalizableConBot = false, Activo = true },
            new Servicio { Id = 11, Categoria = "Otros", Nombre = "Retiro de Esmalte o Uñas", Descripcion = "Retiro seguro de acrílico o semipermanente de otro salón", PrecioBase = 20m, DuracionBaseMinutos = 30, EsPersonalizableConBot = false, Activo = true }
        );
    }
}

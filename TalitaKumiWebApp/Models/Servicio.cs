using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalitaKumiWebApp.Models;

public class Servicio
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Categoria { get; set; } = string.Empty; // "Uñas", "Pedicura", "Mirada", "Otros"

    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Descripcion { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal PrecioBase { get; set; }

    public int DuracionBaseMinutos { get; set; }

    public bool EsPersonalizableConBot { get; set; }

    public bool Activo { get; set; } = true;
}

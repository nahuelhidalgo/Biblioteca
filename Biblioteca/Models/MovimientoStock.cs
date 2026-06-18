using System.ComponentModel.DataAnnotations;

namespace Biblioteca.Modelos;

public class MovimientoStock
{
    public int IdMovimientoStock { get; set; }

    public int IdLibro { get; set; }

    public Libro Libro { get; set; } = null!;

    public int? IdPrestamo { get; set; }

    public int? IdEmpleado { get; set; }

    public Empleado? Empleado { get; set; }

    [Required]
    [EnumDataType(typeof(TipoMovimientoStock))]
    public TipoMovimientoStock? TipoMovimiento { get; set; }

    [Range(1, int.MaxValue)]
    public int Cantidad { get; set; }

    [Required]
    [StringLength(300)]
    public string Motivo { get; set; } = string.Empty;

    public DateTime Fecha { get; set; } = DateTime.Now;
}

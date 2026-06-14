using System.ComponentModel.DataAnnotations;

namespace Biblioteca.Modelos;

public class Prestamo
{
    public int IdPrestamo { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    public DateTime FechaEstimadaDevolucion { get; set; }

    public DateTime? FechaDevolucion { get; set; }

    [Required]
    [StringLength(30)]
    public string Estado { get; set; } = "Activo";

    public int LegajoEmpleado { get; set; }

    public Empleado Empleado { get; set; } = null!;

    public ICollection<ItemPrestamo> ItemsPrestamo { get; set; } = new List<ItemPrestamo>();
}

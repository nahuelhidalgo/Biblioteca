using System.ComponentModel.DataAnnotations;

namespace Biblioteca.ViewModels;

public class PrestamoCreateViewModel
{
    [Required]
    [Display(Name = "Empleado")]
    public int LegajoEmpleado { get; set; }

    [Required]
    [Display(Name = "Libros")]
    public List<int> LibrosIds { get; set; } = new();

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de prestamo")]
    public DateTime FechaPrestamo { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Fecha estimada de devolucion")]
    public DateTime FechaEstimadaDevolucion { get; set; } = DateTime.Today.AddDays(15);
}

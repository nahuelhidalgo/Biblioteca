using System.ComponentModel.DataAnnotations;

namespace Biblioteca.ViewModels;

public class DevolucionPrestamoViewModel
{
    public int IdItemPrestamo { get; set; }

    public string Prestamo { get; set; } = string.Empty;

    public string Libro { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de prestamo")]
    public DateTime FechaPrestamo { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Fecha estimada de devolucion")]
    public DateTime FechaEstimadaDevolucion { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de devolucion")]
    public DateTime FechaDevolucion { get; set; } = DateTime.Today;
}

using System.ComponentModel.DataAnnotations;

namespace Biblioteca.ViewModels;

public class PrestamoCreateViewModel
{
    [Required]
    [Display(Name = "Libros")]
    public List<int> LibrosIds { get; set; } = new();

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de préstamo")]
    public DateTime FechaPrestamo { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de devolución")]
    public DateTime FechaEstimadaDevolucion { get; set; } = DateTime.Today.AddDays(15);
}

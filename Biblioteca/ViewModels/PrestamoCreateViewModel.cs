using System.ComponentModel.DataAnnotations;

namespace Biblioteca.ViewModels;

public class PrestamoCreateViewModel
{
    [Required]
    [Display(Name = "Libros")]
    public List<int> LibrosIds { get; set; } = new();

    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Fecha de préstamo")]
    public DateTime FechaPrestamo { get; set; } = DateTime.Now;

    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Fecha de devolución")]
    public DateTime FechaEstimadaDevolucion { get; set; } = DateTime.Now.AddDays(15);
}

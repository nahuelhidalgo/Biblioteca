using System.ComponentModel.DataAnnotations;

namespace Biblioteca.ViewModels;

public class DevolucionPrestamoViewModel
{
    [Display(Name = "ID de préstamo")]
    [Range(1, int.MaxValue, ErrorMessage = "Ingrese un ID de préstamo válido.")]
    public int IdPrestamo { get; set; }

    public string Prestamo { get; set; } = string.Empty;

    public string Empleado { get; set; } = string.Empty;

    public string Libros { get; set; } = string.Empty;

    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Fecha de préstamo")]
    public DateTime FechaPrestamo { get; set; }

    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Fecha de devolución")]
    public DateTime FechaEstimadaDevolucion { get; set; }

    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Fecha de devolución")]
    public DateTime FechaDevolucion { get; set; } = DateTime.Now;

    public bool EstaVencido { get; set; }

    public int DiasVencido { get; set; }

    public bool YaDevuelto { get; set; }
}

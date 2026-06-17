using System.ComponentModel.DataAnnotations;

namespace Biblioteca.ViewModels;

public class PrestamoCreateViewModel
{
    [Display(Name = "ISBN")]
    public string ISBN { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    public int Cantidad { get; set; } = 1;

    public string? Accion { get; set; }

    public List<PrestamoCarritoItemViewModel> Items { get; set; } = new();

    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Fecha de devolución")]
    public DateTime FechaEstimadaDevolucion { get; set; } = DateTime.Now.AddDays(15);
}

public class PrestamoCarritoItemViewModel
{
    public int IdLibro { get; set; }

    public string ISBN { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public int StockDisponible { get; set; }

    public int Cantidad { get; set; }
}

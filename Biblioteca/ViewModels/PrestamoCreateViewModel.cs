using System.ComponentModel.DataAnnotations;

namespace Biblioteca.ViewModels;

public class PrestamoCreateViewModel
{
    [Required(ErrorMessage = "Seleccione el cliente que recibirá el préstamo.")]
    [Display(Name = "Cliente")]
    public int? IdCliente { get; set; }

    [Display(Name = "ISBN")]
    public string? ISBN { get; set; }

    public int? Cantidad { get; set; } = 1;

    public List<PrestamoCarritoItemViewModel> Items { get; set; } = new();

    [Required(ErrorMessage = "Ingrese la duración del préstamo.")]
    [Range(15, int.MaxValue, ErrorMessage = "La duración mínima del préstamo es de 15 días.")]
    [Display(Name = "Duración del préstamo")]
    public int? DuracionDias { get; set; } = 15;
}

public class PrestamoCarritoItemViewModel
{
    public int IdLibro { get; set; }

    public string ISBN { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public int StockDisponible { get; set; }

    public int Cantidad { get; set; }
}

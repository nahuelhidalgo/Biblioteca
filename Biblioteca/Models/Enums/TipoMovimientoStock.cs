using System.ComponentModel.DataAnnotations;

namespace Biblioteca.Modelos;

public enum TipoMovimientoStock
{
    Prestado = 1,

    [Display(Name = "Baja de Stock")]
    BajaStock = 2,

    [Display(Name = "Alta de Stock")]
    AltaStock = 3
}

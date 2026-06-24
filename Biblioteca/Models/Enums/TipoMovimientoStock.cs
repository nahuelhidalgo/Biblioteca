using System.ComponentModel.DataAnnotations;

namespace Biblioteca.Modelos;

public enum TipoMovimientoStock
{
    Prestado = 1,

    [Display(Name = "Devolución")]
    Devolucion = 2
}

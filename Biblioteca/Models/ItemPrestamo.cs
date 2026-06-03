using System.ComponentModel.DataAnnotations;

namespace Biblioteca.Modelos;

public class ItemPrestamo
{
    public int IdItemPrestamo { get; set; }

    public int IdPrestamo { get; set; }

    public Prestamo Prestamo { get; set; } = null!;

    public int IdLibro { get; set; }

    public Libro Libro { get; set; } = null!;

    public DateTime? FechaDevolucion { get; set; }

    [EnumDataType(typeof(EstadoItemPrestamo))]
    public EstadoItemPrestamo EstadoItem { get; set; } = EstadoItemPrestamo.Prestado;

    [Range(0, int.MaxValue)]
    public int DiasRetraso { get; set; }
}

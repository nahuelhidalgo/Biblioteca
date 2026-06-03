using System.ComponentModel.DataAnnotations;
using Biblioteca.Validaciones;

namespace Biblioteca.Modelos;

public class Libro
{
    public int IdLibro { get; set; }

    [Required]
    [StringLength(17)]
    [Isbn]
    public string ISBN { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Titulo { get; set; } = string.Empty;

    public int IdEditorial { get; set; }

    public Editorial Editorial { get; set; } = null!;

    public int IdCategoria { get; set; }

    public Categoria Categoria { get; set; } = null!;

    public int IdAutor { get; set; }

    public Autor Autor { get; set; } = null!;

    [AnioPublicacion]
    public int AnioPublicacion { get; set; }

    [Range(0, int.MaxValue)]
    public int StockTotal { get; set; }

    [Range(0, int.MaxValue)]
    public int StockDisponible { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<ItemPrestamo> ItemsPrestamo { get; set; } = new List<ItemPrestamo>();

    public ICollection<MovimientoStock> MovimientosStock { get; set; } = new List<MovimientoStock>();
}

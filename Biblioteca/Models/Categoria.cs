using System.ComponentModel.DataAnnotations;

namespace Biblioteca.Modelos;

public class Categoria
{
    public int IdCategoria { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<Libro> Libros { get; set; } = new List<Libro>();
}

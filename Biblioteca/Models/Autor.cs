using System.ComponentModel.DataAnnotations;

namespace Biblioteca.Modelos;

public class Autor
{
    public int IdAutor { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Apellido { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Nacionalidad { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<Libro> Libros { get; set; } = new List<Libro>();
}

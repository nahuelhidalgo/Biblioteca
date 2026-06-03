using System.ComponentModel.DataAnnotations;

namespace Biblioteca.Modelos;

public class Editorial
{
    public int IdEditorial { get; set; }

    [Required]
    [StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string Descripcion { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public ICollection<Libro> Libros { get; set; } = new List<Libro>();
}

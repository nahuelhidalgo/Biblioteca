using System.ComponentModel.DataAnnotations;

namespace Biblioteca.Modelos;

public abstract class Persona
{
    public int IdPersona { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Apellido { get; set; } = string.Empty;

    [Required]
    [StringLength(8, MinimumLength = 7)]
    [RegularExpression(@"^\d{7,8}$", ErrorMessage = "El DNI debe contener 7 u 8 digitos numericos.")]
    public string DNI { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(30)]
    public string Telefono { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Direccion { get; set; } = string.Empty;

    public string NombreCompleto => $"{Nombre} {Apellido}";
}

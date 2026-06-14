using System.ComponentModel.DataAnnotations;

namespace Biblioteca.Modelos;

public class Usuario
{
    public int IdUsuarioSistema { get; set; }

    [Required]
    [Display(Name = "Cuenta")]
    [EmailAddress(ErrorMessage = "Ingrese un email valido.")]
    [RegularExpression(@"^[a-zA-Z0-9]+(\.[a-zA-Z0-9]+)+@[oO][rR][tT]\.[eE][dD][uU]\.[aA][rR]$", ErrorMessage = "La cuenta debe tener el formato nombre.apellido@ort.edu.ar.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Contraseña")]
    [StringLength(255)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string Rol { get; set; } = "Operador";

    public bool Activo { get; set; } = true;

    public DateTime? UltimoAcceso { get; set; }

    public int LegajoEmpleado { get; set; }

    public Empleado Empleado { get; set; } = null!;
}

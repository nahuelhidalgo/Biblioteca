using System.ComponentModel.DataAnnotations;

namespace Biblioteca.Modelos;

public class Usuario
{
    public int IdUsuarioSistema { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
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

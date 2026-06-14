using System.ComponentModel.DataAnnotations;

namespace Biblioteca.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingrese la cuenta.")]
    [Display(Name = "Cuenta")]
    [EmailAddress(ErrorMessage = "Ingrese un email valido.")]
    [RegularExpression(@"^[a-zA-Z0-9]+(\.[a-zA-Z0-9]+)+@[oO][rR][tT]\.[eE][dD][uU]\.[aA][rR]$", ErrorMessage = "La cuenta debe tener el formato nombre.apellido@ort.edu.ar.")]
    public string Cuenta { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese la contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Biblioteca.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingrese la cuenta.")]
    [Display(Name = "Cuenta")]
    public string Cuenta { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese la contrasenia.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contrasenia")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

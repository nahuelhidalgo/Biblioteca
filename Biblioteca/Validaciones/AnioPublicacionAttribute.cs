using System.ComponentModel.DataAnnotations;

namespace Biblioteca.Validaciones;

public class AnioPublicacionAttribute : ValidationAttribute
{
    private const int AnioMinimo = 1000;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is not int anio)
        {
            return new ValidationResult("El año de publicacion no es valido.");
        }

        var anioActual = DateTime.Today.Year;

        if (anio < AnioMinimo || anio > anioActual)
        {
            return new ValidationResult($"El año de publicacion debe estar entre {AnioMinimo} y {anioActual}.");
        }

        return ValidationResult.Success;
    }
}

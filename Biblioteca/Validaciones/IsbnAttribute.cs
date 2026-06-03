using System.ComponentModel.DataAnnotations;

namespace Biblioteca.Validaciones;

public class IsbnAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        var isbn = value.ToString()
            ?.Replace("-", string.Empty)
            .Replace(" ", string.Empty)
            .ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(isbn))
        {
            return ValidationResult.Success;
        }

        if (EsIsbn10(isbn) || EsIsbn13(isbn))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult("El ISBN debe tener formato ISBN-10 o ISBN-13 valido.");
    }

    private static bool EsIsbn10(string isbn)
    {
        if (isbn.Length != 10)
        {
            return false;
        }

        var suma = 0;

        for (var i = 0; i < 10; i++)
        {
            var caracter = isbn[i];
            int valor;

            if (i == 9 && caracter == 'X')
            {
                valor = 10;
            }
            else if (char.IsDigit(caracter))
            {
                valor = caracter - '0';
            }
            else
            {
                return false;
            }

            suma += valor * (10 - i);
        }

        return suma % 11 == 0;
    }

    private static bool EsIsbn13(string isbn)
    {
        if (isbn.Length != 13 || !isbn.All(char.IsDigit))
        {
            return false;
        }

        var suma = 0;

        for (var i = 0; i < 13; i++)
        {
            var digito = isbn[i] - '0';
            suma += i % 2 == 0 ? digito : digito * 3;
        }

        return suma % 10 == 0;
    }
}

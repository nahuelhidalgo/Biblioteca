using Biblioteca.Modelos;
using System.Globalization;
using System.Text;

namespace Biblioteca.Seguridad;

public static class CuentasEmpleado
{
    public static string CrearEmail(Empleado empleado)
    {
        var partes = ObtenerPartesNormalizadas(empleado.Nombre)
            .Concat(ObtenerPartesNormalizadas(empleado.Apellido));
        var cuenta = string.Join(".", partes);

        return $"{cuenta}@ort.edu.ar";
    }

    public static string CrearPasswordInicial(Empleado empleado)
    {
        var partes = ObtenerPartesNormalizadas(empleado.Nombre)
            .Concat(ObtenerPartesNormalizadas(empleado.Apellido));
        var cuenta = string.Concat(partes);

        return $"{cuenta}.inicial";
    }

    private static IEnumerable<string> ObtenerPartesNormalizadas(string texto)
    {
        var normalizado = (texto ?? string.Empty).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var caracter in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(caracter))
            {
                builder.Append(caracter);
                continue;
            }

            if (builder.Length > 0)
            {
                yield return builder.ToString();
                builder.Clear();
            }
        }

        if (builder.Length > 0)
        {
            yield return builder.ToString();
        }
    }
}

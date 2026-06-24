namespace Biblioteca.Modelos;

public class Cliente : Persona
{
    [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Ingrese un email válido.")]
    [System.ComponentModel.DataAnnotations.StringLength(150)]
    public string? Email { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
}

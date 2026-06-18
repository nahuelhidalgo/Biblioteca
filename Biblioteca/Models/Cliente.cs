namespace Biblioteca.Modelos;

public class Cliente : Persona
{
    public bool Activo { get; set; } = true;

    public ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
}

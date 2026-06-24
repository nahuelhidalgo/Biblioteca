namespace Biblioteca.Modelos;

public class Empleado : Persona
{
    public bool Activo { get; set; } = true;

    public Usuario? Usuario { get; set; }

    public ICollection<Prestamo> PrestamosRegistrados { get; set; } = new List<Prestamo>();

    public ICollection<MovimientoStock> MovimientosStock { get; set; } = new List<MovimientoStock>();
}

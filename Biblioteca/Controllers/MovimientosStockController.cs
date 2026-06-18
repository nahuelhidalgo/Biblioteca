using Biblioteca.Datos;
using Biblioteca.Filtros;
using Biblioteca.Modelos;
using Biblioteca.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Controllers;

[SesionAuthorize(RolesSistema.Administrador)]
public class MovimientosStockController : Controller
{
    private readonly BibliotecaContext _context;

    public MovimientosStockController(BibliotecaContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(
        int? idEmpleado,
        DateTime? fechaCreacion,
        int? idLibro,
        TipoMovimientoStock? tipoMovimiento,
        string? orden,
        string? direccion)
    {
        var movimientosQuery = _context.MovimientosStock
            .Include(m => m.Libro)
            .Include(m => m.Empleado)
            .AsQueryable();

        if (idEmpleado.HasValue)
        {
            movimientosQuery = movimientosQuery.Where(m => m.IdEmpleado == idEmpleado.Value);
        }

        if (fechaCreacion.HasValue)
        {
            var desde = fechaCreacion.Value.Date;
            var hasta = desde.AddDays(1);
            movimientosQuery = movimientosQuery.Where(m => m.Fecha >= desde && m.Fecha < hasta);
        }

        if (idLibro.HasValue)
        {
            movimientosQuery = movimientosQuery.Where(m => m.IdLibro == idLibro.Value);
        }

        if (tipoMovimiento.HasValue)
        {
            movimientosQuery = movimientosQuery.Where(m => m.TipoMovimiento == tipoMovimiento.Value);
        }

        var criterioOrden = string.Equals(orden, "cantidad", StringComparison.OrdinalIgnoreCase)
            ? "cantidad"
            : "fecha";
        var ascendente = string.Equals(direccion, "asc", StringComparison.OrdinalIgnoreCase);

        movimientosQuery = criterioOrden == "cantidad"
            ? ascendente
                ? movimientosQuery.OrderBy(m => m.Cantidad).ThenByDescending(m => m.Fecha)
                : movimientosQuery.OrderByDescending(m => m.Cantidad).ThenByDescending(m => m.Fecha)
            : ascendente
                ? movimientosQuery.OrderBy(m => m.Fecha).ThenBy(m => m.IdMovimientoStock)
                : movimientosQuery.OrderByDescending(m => m.Fecha).ThenByDescending(m => m.IdMovimientoStock);

        await CargarFiltrosAsync(idEmpleado, idLibro);

        ViewBag.FechaCreacionFiltro = fechaCreacion?.ToString("yyyy-MM-dd");
        ViewBag.TipoMovimientoFiltro = tipoMovimiento?.ToString();
        ViewBag.Orden = criterioOrden;
        ViewBag.Direccion = ascendente ? "asc" : "desc";

        var movimientos = await movimientosQuery
            .AsNoTracking()
            .ToListAsync();

        return View(movimientos);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var movimiento = await _context.MovimientosStock
            .Include(m => m.Libro)
            .ThenInclude(l => l.Autor)
            .Include(m => m.Libro)
            .ThenInclude(l => l.Categoria)
            .Include(m => m.Libro)
            .ThenInclude(l => l.Editorial)
            .Include(m => m.Empleado)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.IdMovimientoStock == id);

        return movimiento is null ? NotFound() : View(movimiento);
    }

    public IActionResult Create()
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create([Bind("IdLibro,TipoMovimiento,Cantidad,Motivo")] MovimientoStock movimiento)
    {
        return RedirectToAction(nameof(Index));
    }

    private async Task CargarFiltrosAsync(int? idEmpleado, int? idLibro)
    {
        var empleados = await _context.Empleados
            .OrderBy(e => e.Nombre)
            .ThenBy(e => e.Apellido)
            .Select(e => new
            {
                e.IdPersona,
                NombreCompleto = $"{e.Nombre} {e.Apellido}"
            })
            .ToListAsync();

        var libros = await _context.Libros
            .OrderBy(l => l.Titulo)
            .Select(l => new
            {
                l.IdLibro,
                Descripcion = $"{l.Titulo} ({l.ISBN})"
            })
            .ToListAsync();

        ViewBag.Empleados = new SelectList(empleados, "IdPersona", "NombreCompleto", idEmpleado);
        ViewBag.Libros = new SelectList(libros, "IdLibro", "Descripcion", idLibro);
        ViewBag.IdEmpleadoFiltro = idEmpleado;
        ViewBag.IdLibroFiltro = idLibro;
    }
}

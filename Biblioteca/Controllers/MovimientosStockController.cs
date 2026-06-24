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
        int? idCliente,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int? idLibro,
        TipoMovimientoStock? tipoMovimiento,
        string? orden,
        string? direccion)
    {
        var movimientosQuery = _context.MovimientosStock
            .Include(m => m.Libro)
            .Include(m => m.Empleado)
            .Include(m => m.Prestamo)
            .ThenInclude(p => p!.Cliente)
            .AsQueryable();

        if (idEmpleado.HasValue)
        {
            movimientosQuery = movimientosQuery.Where(m => m.IdEmpleado == idEmpleado.Value);
        }

        if (idCliente.HasValue)
        {
            movimientosQuery = movimientosQuery.Where(m =>
                m.Prestamo != null && m.Prestamo.IdCliente == idCliente.Value);
        }

        if (fechaDesde.HasValue)
        {
            movimientosQuery = movimientosQuery.Where(m => m.Fecha >= fechaDesde.Value.Date);
        }

        if (fechaHasta.HasValue)
        {
            var hasta = fechaHasta.Value.Date.AddDays(1);
            movimientosQuery = movimientosQuery.Where(m => m.Fecha < hasta);
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

        await CargarFiltrosAsync(idEmpleado, idCliente, idLibro);

        ViewBag.FechaDesdeFiltro = fechaDesde?.ToString("yyyy-MM-dd");
        ViewBag.FechaHastaFiltro = fechaHasta?.ToString("yyyy-MM-dd");
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
            .Include(m => m.Prestamo)
            .ThenInclude(p => p!.Cliente)
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

    private async Task CargarFiltrosAsync(int? idEmpleado, int? idCliente, int? idLibro)
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

        var clientes = await _context.Clientes
            .OrderBy(c => c.Apellido)
            .ThenBy(c => c.Nombre)
            .Select(c => new
            {
                c.IdPersona,
                Descripcion = $"{c.Nombre} {c.Apellido} - DNI {c.DNI}"
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
        ViewBag.Clientes = new SelectList(clientes, "IdPersona", "Descripcion", idCliente);
        ViewBag.Libros = new SelectList(libros, "IdLibro", "Descripcion", idLibro);
        ViewBag.IdEmpleadoFiltro = idEmpleado;
        ViewBag.IdClienteFiltro = idCliente;
        ViewBag.IdLibroFiltro = idLibro;
    }
}

using Biblioteca.Datos;
using Biblioteca.Filtros;
using Biblioteca.Modelos;
using Biblioteca.Seguridad;
using Microsoft.AspNetCore.Mvc;
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

    public async Task<IActionResult> Index()
    {
        var movimientos = await _context.MovimientosStock
            .Include(m => m.Libro)
            .Include(m => m.Empleado)
            .OrderByDescending(m => m.Fecha)
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
}

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

    public async Task<IActionResult> Index()
    {
        var movimientos = await _context.MovimientosStock
            .Include(m => m.Libro)
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
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.IdMovimientoStock == id);

        return movimiento is null ? NotFound() : View(movimiento);
    }

    public async Task<IActionResult> Create()
    {
        await CargarLibrosAsync();
        return View(new MovimientoStock { Fecha = DateTime.Now });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdLibro,TipoMovimiento,Cantidad,Motivo")] MovimientoStock movimiento)
    {
        if (movimiento.TipoMovimiento == TipoMovimientoStock.Prestado)
        {
            ModelState.AddModelError(nameof(MovimientoStock.TipoMovimiento), "El movimiento Prestado se genera desde Prestamos.");
        }

        if (!ModelState.IsValid)
        {
            await CargarLibrosAsync(movimiento.IdLibro);
            return View(movimiento);
        }

        var libro = await _context.Libros.FirstOrDefaultAsync(l => l.IdLibro == movimiento.IdLibro);

        if (libro is null)
        {
            return NotFound();
        }

        if (movimiento.TipoMovimiento == TipoMovimientoStock.AltaStock)
        {
            libro.StockTotal += movimiento.Cantidad;
            libro.StockDisponible += movimiento.Cantidad;
        }
        else if (movimiento.TipoMovimiento == TipoMovimientoStock.BajaStock)
        {
            if (libro.StockDisponible < movimiento.Cantidad)
            {
                ModelState.AddModelError(nameof(MovimientoStock.Cantidad), "No se puede registrar una baja mayor al stock disponible.");
                await CargarLibrosAsync(movimiento.IdLibro);
                return View(movimiento);
            }

            libro.StockTotal -= movimiento.Cantidad;
            libro.StockDisponible -= movimiento.Cantidad;
        }

        movimiento.Fecha = DateTime.Now;
        _context.MovimientosStock.Add(movimiento);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task CargarLibrosAsync(int? seleccionado = null)
    {
        var libros = await _context.Libros
            .Where(l => l.Activo)
            .OrderBy(l => l.Titulo)
            .Select(l => new
            {
                l.IdLibro,
                Descripcion = $"{l.Titulo} - Disponible: {l.StockDisponible}"
            })
            .ToListAsync();

        ViewBag.IdLibro = new SelectList(libros, "IdLibro", "Descripcion", seleccionado);
    }
}

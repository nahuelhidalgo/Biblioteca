using Biblioteca.Datos;
using Biblioteca.Filtros;
using Biblioteca.Modelos;
using Biblioteca.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Controllers;

[SesionAuthorize]
public class LibrosController : Controller
{
    private readonly BibliotecaContext _context;

    public LibrosController(BibliotecaContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? busqueda)
    {
        var query = _context.Libros
            .Include(l => l.Autor)
            .Include(l => l.Categoria)
            .Include(l => l.Editorial)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            query = query.Where(l =>
                l.Titulo.Contains(busqueda)
                || l.ISBN.Contains(busqueda)
                || l.Autor.Nombre.Contains(busqueda)
                || l.Autor.Apellido.Contains(busqueda)
                || l.Categoria.Nombre.Contains(busqueda));
        }

        ViewBag.Busqueda = busqueda;

        return View(await query.ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var libro = await _context.Libros
            .Include(l => l.Autor)
            .Include(l => l.Categoria)
            .Include(l => l.Editorial)
            .Include(l => l.MovimientosStock.OrderByDescending(m => m.Fecha))
            .ThenInclude(m => m.Prestamo)
            .ThenInclude(p => p!.Cliente)
            .Include(l => l.MovimientosStock.OrderByDescending(m => m.Fecha))
            .ThenInclude(m => m.Empleado)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.IdLibro == id);

        return libro is null ? NotFound() : View(libro);
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    public async Task<IActionResult> Create()
    {
        await CargarListasAsync();
        return View();
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdLibro,ISBN,Titulo,IdEditorial,IdCategoria,IdAutor,AnioPublicacion,Activo")] Libro libro)
    {
        libro.ISBN = libro.ISBN.Trim();
        libro.StockTotal = 0;
        libro.StockDisponible = 0;
        LimpiarValidacionesDeNavegacion();
        await ValidarIsbnUnicoAsync(libro.ISBN);

        if (!ModelState.IsValid)
        {
            await CargarListasAsync(libro);
            return View(libro);
        }

        _context.Add(libro);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var libro = await _context.Libros.FindAsync(id);

        if (libro is null)
        {
            return NotFound();
        }

        await CargarListasAsync(libro);
        return View(libro);
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("IdLibro,ISBN,Titulo,IdEditorial,IdCategoria,IdAutor,AnioPublicacion,StockTotal,StockDisponible,Activo")] Libro libro)
    {
        if (id != libro.IdLibro)
        {
            return NotFound();
        }

        libro.ISBN = libro.ISBN.Trim();
        LimpiarValidacionesDeNavegacion();
        await ValidarIsbnUnicoAsync(libro.ISBN, libro.IdLibro);

        if (!ModelState.IsValid)
        {
            await CargarListasAsync(libro);
            return View(libro);
        }

        _context.Update(libro);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var libro = await _context.Libros
            .Include(l => l.Autor)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.IdLibro == id);

        return libro is null ? NotFound() : View(libro);
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var libro = await _context.Libros.FindAsync(id);

        if (libro is not null)
        {
            libro.Activo = false;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activar(int id)
    {
        var libro = await _context.Libros.FindAsync(id);

        if (libro is not null)
        {
            libro.Activo = true;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task CargarListasAsync(Libro? libro = null)
    {
        ViewBag.IdAutor = new SelectList(
            await _context.Autores
                .Where(a => a.Activo)
                .OrderBy(a => a.Nombre)
                .ThenBy(a => a.Apellido)
                .Select(a => new
                {
                    a.IdAutor,
                    NombreCompleto = $"{a.Nombre} {a.Apellido}"
                })
                .ToListAsync(),
            "IdAutor",
            "NombreCompleto",
            libro?.IdAutor);

        ViewBag.IdCategoria = new SelectList(
            await _context.Categorias.Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync(),
            "IdCategoria",
            "Nombre",
            libro?.IdCategoria);

        ViewBag.IdEditorial = new SelectList(
            await _context.Editoriales.Where(e => e.Activo).OrderBy(e => e.Nombre).ToListAsync(),
            "IdEditorial",
            "Nombre",
            libro?.IdEditorial);
    }

    private void LimpiarValidacionesDeNavegacion()
    {
        ModelState.Remove(nameof(Libro.Autor));
        ModelState.Remove(nameof(Libro.Categoria));
        ModelState.Remove(nameof(Libro.Editorial));
        ModelState.Remove(nameof(Libro.ItemsPrestamo));
        ModelState.Remove(nameof(Libro.MovimientosStock));
    }

    private async Task ValidarIsbnUnicoAsync(string isbn, int? idLibroActual = null)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return;
        }

        var isbnEnUso = await _context.Libros.AnyAsync(l =>
            l.ISBN == isbn && (!idLibroActual.HasValue || l.IdLibro != idLibroActual.Value));

        if (isbnEnUso)
        {
            ModelState.AddModelError(nameof(Libro.ISBN), "Ya existe un libro registrado con ese ISBN.");
        }
    }
}

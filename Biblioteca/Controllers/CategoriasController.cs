using Biblioteca.Datos;
using Biblioteca.Filtros;
using Biblioteca.Modelos;
using Biblioteca.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Controllers;

[SesionAuthorize]
public class CategoriasController : Controller
{
    private readonly BibliotecaContext _context;

    public CategoriasController(BibliotecaContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Categorias.AsNoTracking().ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var categoria = await _context.Categorias
            .Include(c => c.Libros)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.IdCategoria == id);

        return categoria is null ? NotFound() : View(categoria);
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    public IActionResult Create(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = ObtenerReturnUrlLocal(returnUrl);
        return View(new Categoria { Activo = true });
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdCategoria,Nombre,Descripcion,Activo")] Categoria categoria, string? returnUrl = null)
    {
        var returnUrlLocal = ObtenerReturnUrlLocal(returnUrl);

        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrlLocal;
            return View(categoria);
        }

        _context.Add(categoria);
        await _context.SaveChangesAsync();
        return !string.IsNullOrWhiteSpace(returnUrlLocal)
            ? LocalRedirect(returnUrlLocal)
            : RedirectToAction(nameof(Index));
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var categoria = await _context.Categorias.FindAsync(id);
        return categoria is null ? NotFound() : View(categoria);
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("IdCategoria,Nombre,Descripcion,Activo")] Categoria categoria)
    {
        if (id != categoria.IdCategoria)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(categoria);
        }

        _context.Update(categoria);
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

        var categoria = await _context.Categorias.AsNoTracking().FirstOrDefaultAsync(c => c.IdCategoria == id);
        return categoria is null ? NotFound() : View(categoria);
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var categoria = await _context.Categorias.FindAsync(id);

        if (categoria is not null)
        {
            categoria.Activo = false;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activar(int id)
    {
        var categoria = await _context.Categorias.FindAsync(id);

        if (categoria is not null)
        {
            categoria.Activo = true;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private string? ObtenerReturnUrlLocal(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : null;
    }
}

using Biblioteca.Datos;
using Biblioteca.Filtros;
using Biblioteca.Modelos;
using Biblioteca.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Controllers;

[SesionAuthorize]
public class AutoresController : Controller
{
    private readonly BibliotecaContext _context;

    public AutoresController(BibliotecaContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Autores.AsNoTracking().ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var autor = await _context.Autores
            .Include(a => a.Libros)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.IdAutor == id);

        return autor is null ? NotFound() : View(autor);
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    public IActionResult Create(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = ObtenerReturnUrlLocal(returnUrl);
        return View(new Autor { Activo = true });
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdAutor,Nombre,Apellido,Nacionalidad,Activo")] Autor autor, string? returnUrl = null)
    {
        var returnUrlLocal = ObtenerReturnUrlLocal(returnUrl);

        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrlLocal;
            return View(autor);
        }

        _context.Add(autor);
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

        var autor = await _context.Autores.FindAsync(id);
        return autor is null ? NotFound() : View(autor);
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("IdAutor,Nombre,Apellido,Nacionalidad,Activo")] Autor autor)
    {
        if (id != autor.IdAutor)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(autor);
        }

        _context.Update(autor);
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

        var autor = await _context.Autores.AsNoTracking().FirstOrDefaultAsync(a => a.IdAutor == id);
        return autor is null ? NotFound() : View(autor);
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var autor = await _context.Autores.FindAsync(id);

        if (autor is not null)
        {
            autor.Activo = false;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activar(int id)
    {
        var autor = await _context.Autores.FindAsync(id);

        if (autor is not null)
        {
            autor.Activo = true;
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

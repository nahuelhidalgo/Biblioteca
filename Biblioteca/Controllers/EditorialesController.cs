using Biblioteca.Datos;
using Biblioteca.Filtros;
using Biblioteca.Modelos;
using Biblioteca.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Controllers;

[SesionAuthorize]
public class EditorialesController : Controller
{
    private readonly BibliotecaContext _context;

    public EditorialesController(BibliotecaContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Editoriales.AsNoTracking().ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var editorial = await _context.Editoriales
            .Include(e => e.Libros)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdEditorial == id);

        return editorial is null ? NotFound() : View(editorial);
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    public IActionResult Create(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = ObtenerReturnUrlLocal(returnUrl);
        return View(new Editorial { Activo = true });
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdEditorial,Nombre,Descripcion,Activo")] Editorial editorial, string? returnUrl = null)
    {
        var returnUrlLocal = ObtenerReturnUrlLocal(returnUrl);

        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrlLocal;
            return View(editorial);
        }

        _context.Add(editorial);
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

        var editorial = await _context.Editoriales.FindAsync(id);
        return editorial is null ? NotFound() : View(editorial);
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("IdEditorial,Nombre,Descripcion,Activo")] Editorial editorial)
    {
        if (id != editorial.IdEditorial)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(editorial);
        }

        _context.Update(editorial);
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

        var editorial = await _context.Editoriales.AsNoTracking().FirstOrDefaultAsync(e => e.IdEditorial == id);
        return editorial is null ? NotFound() : View(editorial);
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var editorial = await _context.Editoriales.FindAsync(id);

        if (editorial is not null)
        {
            editorial.Activo = false;
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

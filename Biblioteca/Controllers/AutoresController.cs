using Biblioteca.Datos;
using Biblioteca.Modelos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Controllers;

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

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdAutor,Nombre,Apellido,Nacionalidad,Activo")] Autor autor)
    {
        if (!ModelState.IsValid)
        {
            return View(autor);
        }

        _context.Add(autor);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var autor = await _context.Autores.FindAsync(id);
        return autor is null ? NotFound() : View(autor);
    }

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

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var autor = await _context.Autores.AsNoTracking().FirstOrDefaultAsync(a => a.IdAutor == id);
        return autor is null ? NotFound() : View(autor);
    }

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
}

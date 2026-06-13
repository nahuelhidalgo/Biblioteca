using Biblioteca.Datos;
using Biblioteca.Filtros;
using Biblioteca.Modelos;
using Biblioteca.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Controllers;

[SesionAuthorize(RolesSistema.Administrador)]
public class UsuariosController : Controller
{
    private readonly BibliotecaContext _context;

    public UsuariosController(BibliotecaContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var usuarios = await _context.Usuarios
            .Include(u => u.Empleado)
            .AsNoTracking()
            .ToListAsync();

        return View(usuarios);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var usuario = await _context.Usuarios
            .Include(u => u.Empleado)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdUsuarioSistema == id);

        return usuario is null ? NotFound() : View(usuario);
    }

    public async Task<IActionResult> Create()
    {
        await CargarEmpleadosAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdUsuarioSistema,Email,Password,Rol,Activo,UltimoAcceso,LegajoEmpleado")] Usuario usuario)
    {
        if (!ModelState.IsValid)
        {
            await CargarEmpleadosAsync(usuario.LegajoEmpleado);
            return View(usuario);
        }

        _context.Add(usuario);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario is null)
        {
            return NotFound();
        }

        await CargarEmpleadosAsync(usuario.LegajoEmpleado);
        return View(usuario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("IdUsuarioSistema,Email,Password,Rol,Activo,UltimoAcceso,LegajoEmpleado")] Usuario usuario)
    {
        if (id != usuario.IdUsuarioSistema)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await CargarEmpleadosAsync(usuario.LegajoEmpleado);
            return View(usuario);
        }

        _context.Update(usuario);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var usuario = await _context.Usuarios
            .Include(u => u.Empleado)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdUsuarioSistema == id);

        return usuario is null ? NotFound() : View(usuario);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario is not null)
        {
            usuario.Activo = false;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task CargarEmpleadosAsync(int? seleccionado = null)
    {
        var empleados = await _context.Empleados
            .OrderBy(e => e.Apellido)
            .ThenBy(e => e.Nombre)
            .Select(e => new
            {
                e.Legajo,
                NombreCompleto = $"{e.Apellido}, {e.Nombre} ({e.Legajo})"
            })
            .ToListAsync();

        ViewBag.LegajoEmpleado = new SelectList(empleados, "Legajo", "NombreCompleto", seleccionado);
    }
}

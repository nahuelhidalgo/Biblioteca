using Biblioteca.Datos;
using Biblioteca.Filtros;
using Biblioteca.Modelos;
using Biblioteca.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Controllers;

[SesionAuthorize(RolesSistema.Administrador)]
public class EmpleadosController : Controller
{
    private readonly BibliotecaContext _context;

    public EmpleadosController(BibliotecaContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var empleados = await _context.Empleados
            .Include(e => e.Usuario)
            .AsNoTracking()
            .ToListAsync();

        return View(empleados);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var empleado = await _context.Empleados
            .Include(e => e.Usuario)
            .Include(e => e.Prestamos)
            .Include(e => e.MovimientosStock)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Legajo == id);

        return empleado is null ? NotFound() : View(empleado);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Legajo,Nombre,Apellido,DNI,Telefono,Direccion")] Empleado empleado)
    {
        var cuenta = CuentasEmpleado.CrearEmail(empleado);
        var passwordInicial = CuentasEmpleado.CrearPasswordInicial(empleado);

        if (await _context.Usuarios.AnyAsync(u => u.Email == cuenta))
        {
            ModelState.AddModelError(string.Empty, $"Ya existe un usuario con la cuenta {cuenta}.");
        }

        if (!ModelState.IsValid)
        {
            return View(empleado);
        }

        empleado.Usuario = new Usuario
        {
            Email = cuenta,
            Password = passwordInicial,
            Rol = RolesSistema.Operador,
            Activo = true
        };

        _context.Empleados.Add(empleado);
        await _context.SaveChangesAsync();

        TempData["Mensaje"] = $"Empleado creado con usuario {cuenta} y contraseña inicial {passwordInicial}.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var empleado = await _context.Empleados.FindAsync(id);
        return empleado is null ? NotFound() : View(empleado);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Legajo,Nombre,Apellido,DNI,Telefono,Direccion")] Empleado empleado)
    {
        if (id != empleado.Legajo)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(empleado);
        }

        _context.Update(empleado);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var empleado = await _context.Empleados
            .Include(e => e.Usuario)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Legajo == id);

        return empleado is null ? NotFound() : View(empleado);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var empleado = await _context.Empleados
            .Include(e => e.Usuario)
            .Include(e => e.Prestamos)
            .Include(e => e.MovimientosStock)
            .FirstOrDefaultAsync(e => e.Legajo == id);

        if (empleado is null)
        {
            return RedirectToAction(nameof(Index));
        }

        if (empleado.Usuario is not null || empleado.Prestamos.Any() || empleado.MovimientosStock.Any())
        {
            ModelState.AddModelError(string.Empty, "No se puede eliminar un empleado con usuario, prestamos o movimientos asociados.");
            return View("Delete", empleado);
        }

        _context.Empleados.Remove(empleado);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}

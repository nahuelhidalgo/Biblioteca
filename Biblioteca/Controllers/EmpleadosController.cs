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
            .Include(e => e.PrestamosRegistrados)
            .Include(e => e.MovimientosStock)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdPersona == id);

        return empleado is null ? NotFound() : View(empleado);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdPersona,Nombre,Apellido,DNI,Telefono,Direccion")] Empleado empleado)
    {
        await ValidarDniUnicoAsync(empleado.DNI);

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

        empleado.Activo = true;
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
    public async Task<IActionResult> Edit(int id, [Bind("IdPersona,Nombre,Apellido,DNI,Telefono,Direccion")] Empleado empleado)
    {
        if (id != empleado.IdPersona)
        {
            return NotFound();
        }

        await ValidarDniUnicoAsync(empleado.DNI, empleado.IdPersona);

        if (!ModelState.IsValid)
        {
            return View(empleado);
        }

        var empleadoExistente = await _context.Empleados.FindAsync(id);

        if (empleadoExistente is null)
        {
            return NotFound();
        }

        empleadoExistente.Nombre = empleado.Nombre;
        empleadoExistente.Apellido = empleado.Apellido;
        empleadoExistente.DNI = empleado.DNI;
        empleadoExistente.Telefono = empleado.Telefono;
        empleadoExistente.Direccion = empleado.Direccion;
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
            .FirstOrDefaultAsync(e => e.IdPersona == id);

        return empleado is null ? NotFound() : View(empleado);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var empleado = await _context.Empleados
            .Include(e => e.Usuario)
            .Include(e => e.PrestamosRegistrados)
            .Include(e => e.MovimientosStock)
            .FirstOrDefaultAsync(e => e.IdPersona == id);

        if (empleado is null)
        {
            return RedirectToAction(nameof(Index));
        }

        if (empleado.Usuario?.Activo == true)
        {
            ModelState.AddModelError(string.Empty, "Para eliminar el empleado, primero debe inactivar su usuario.");
            return View("Delete", empleado);
        }

        empleado.Activo = false;
        await _context.SaveChangesAsync();
        TempData["Mensaje"] = "Empleado eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activar(int id)
    {
        var empleado = await _context.Empleados.FindAsync(id);

        if (empleado is not null)
        {
            empleado.Activo = true;
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Empleado activado correctamente.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task ValidarDniUnicoAsync(string dni, int? idPersonaActual = null)
    {
        if (string.IsNullOrWhiteSpace(dni))
        {
            return;
        }

        var dniEnUso = await _context.Personas.AnyAsync(p =>
            p.DNI == dni && (!idPersonaActual.HasValue || p.IdPersona != idPersonaActual.Value));

        if (dniEnUso)
        {
            ModelState.AddModelError(nameof(Persona.DNI), "Ya existe una persona registrada con ese DNI.");
        }
    }
}

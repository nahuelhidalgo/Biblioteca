using Biblioteca.Datos;
using Biblioteca.Filtros;
using Biblioteca.Modelos;
using Biblioteca.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

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
        await CargarEmpleadosDisponiblesAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdUsuarioSistema,Email,Password,Rol,Activo,UltimoAcceso,LegajoEmpleado")] Usuario usuario)
    {
        usuario.Email = usuario.Email.Trim().ToLowerInvariant();
        var empleado = await _context.Empleados
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Legajo == usuario.LegajoEmpleado);

        if (empleado is null)
        {
            ModelState.AddModelError(nameof(Usuario.LegajoEmpleado), "Seleccione un empleado valido.");
        }
        else
        {
            ValidarCuentaEmpleado(usuario, empleado);
        }

        if (await _context.Usuarios.AnyAsync(u => u.LegajoEmpleado == usuario.LegajoEmpleado))
        {
            ModelState.AddModelError(nameof(Usuario.LegajoEmpleado), "El empleado seleccionado ya tiene un usuario asociado.");
        }

        if (!ModelState.IsValid)
        {
            await CargarEmpleadosDisponiblesAsync(usuario.LegajoEmpleado);
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

        await CargarEmpleadosDisponiblesAsync(usuario.LegajoEmpleado, usuario.IdUsuarioSistema);
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

        usuario.Email = usuario.Email.Trim().ToLowerInvariant();
        var empleado = await _context.Empleados
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Legajo == usuario.LegajoEmpleado);

        if (empleado is null)
        {
            ModelState.AddModelError(nameof(Usuario.LegajoEmpleado), "Seleccione un empleado valido.");
        }
        else
        {
            ValidarCuentaEmpleado(usuario, empleado);
        }

        if (await _context.Usuarios.AnyAsync(u =>
            u.IdUsuarioSistema != usuario.IdUsuarioSistema && u.LegajoEmpleado == usuario.LegajoEmpleado))
        {
            ModelState.AddModelError(nameof(Usuario.LegajoEmpleado), "El empleado seleccionado ya tiene un usuario asociado.");
        }

        if (!ModelState.IsValid)
        {
            await CargarEmpleadosDisponiblesAsync(usuario.LegajoEmpleado, usuario.IdUsuarioSistema);
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

    private async Task CargarEmpleadosDisponiblesAsync(int? seleccionado = null, int? usuarioActualId = null)
    {
        var empleados = await _context.Empleados
            .Where(e => e.Usuario == null
                || (usuarioActualId != null && e.Usuario.IdUsuarioSistema == usuarioActualId))
            .OrderBy(e => e.Nombre)
            .ThenBy(e => e.Apellido)
            .Select(e => new
            {
                e.Legajo,
                NombreCompleto = $"{e.Nombre} {e.Apellido} ({e.Legajo})"
            })
            .ToListAsync();

        ViewBag.LegajoEmpleado = new SelectList(empleados, "Legajo", "NombreCompleto", seleccionado);
    }

    private void ValidarCuentaEmpleado(Usuario usuario, Empleado empleado)
    {
        var cuentaEsperada = CrearCuentaEsperada(empleado);

        if (!string.Equals(usuario.Email, cuentaEsperada, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(Usuario.Email),
                $"La cuenta debe ser {cuentaEsperada} para el empleado seleccionado.");
        }
    }

    private static string CrearCuentaEsperada(Empleado empleado)
    {
        var partesNombre = NormalizarParteCuenta(empleado.Nombre)
            .Split('.', StringSplitOptions.RemoveEmptyEntries);
        var apellido = NormalizarParteCuenta(empleado.Apellido);
        var usuario = string.Join(".", partesNombre.Append(apellido));

        return $"{usuario}@ort.edu.ar";
    }

    private static string NormalizarParteCuenta(string texto)
    {
        var normalizado = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        var agregarPunto = false;

        foreach (var caracter in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(caracter))
            {
                if (agregarPunto && builder.Length > 0)
                {
                    builder.Append('.');
                }

                builder.Append(caracter);
                agregarPunto = false;
            }
            else if (char.IsWhiteSpace(caracter) || caracter == '.' || caracter == '-' || caracter == '_')
            {
                agregarPunto = builder.Length > 0;
            }
        }

        return builder.ToString();
    }
}

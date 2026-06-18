using Biblioteca.Datos;
using Biblioteca.Filtros;
using Biblioteca.Modelos;
using Biblioteca.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;

namespace Biblioteca.Controllers;

[SesionAuthorize]
public class ClientesController : Controller
{
    private readonly BibliotecaContext _context;

    public ClientesController(BibliotecaContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var clientesQuery = _context.Clientes.AsQueryable();

        if (!EsAdministrador())
        {
            clientesQuery = clientesQuery.Where(c => c.Activo);
        }

        var clientes = await clientesQuery
            .Include(c => c.Prestamos)
            .OrderBy(c => c.Apellido)
            .ThenBy(c => c.Nombre)
            .AsNoTracking()
            .ToListAsync();

        return View(clientes);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var esAdministrador = EsAdministrador();
        var cliente = await _context.Clientes
            .Include(c => c.Prestamos)
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.IdPersona == id && (c.Activo || esAdministrador));

        return cliente is null ? NotFound() : View(cliente);
    }

    public IActionResult Create(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = ObtenerReturnUrlLocal(returnUrl);
        return View(new Cliente { Activo = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Nombre,Apellido,DNI,Telefono,Direccion")] Cliente cliente,
        string? returnUrl = null)
    {
        var returnUrlLocal = ObtenerReturnUrlLocal(returnUrl);
        await ValidarDniUnicoAsync(cliente.DNI);

        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrlLocal;
            return View(cliente);
        }

        cliente.Activo = true;
        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();

        TempData["Mensaje"] = "Cliente creado correctamente.";
        return !string.IsNullOrWhiteSpace(returnUrlLocal)
            ? LocalRedirect(QueryHelpers.AddQueryString(
                returnUrlLocal,
                "idCliente",
                cliente.IdPersona.ToString()))
            : RedirectToAction(nameof(Index));
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var cliente = await _context.Clientes.FindAsync(id);
        return cliente is null ? NotFound() : View(cliente);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [SesionAuthorize(RolesSistema.Administrador)]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("IdPersona,Nombre,Apellido,DNI,Telefono,Direccion")] Cliente cliente)
    {
        if (id != cliente.IdPersona)
        {
            return NotFound();
        }

        await ValidarDniUnicoAsync(cliente.DNI, cliente.IdPersona);

        if (!ModelState.IsValid)
        {
            return View(cliente);
        }

        var clienteExistente = await _context.Clientes.FindAsync(id);

        if (clienteExistente is null)
        {
            return NotFound();
        }

        clienteExistente.Nombre = cliente.Nombre;
        clienteExistente.Apellido = cliente.Apellido;
        clienteExistente.DNI = cliente.DNI;
        clienteExistente.Telefono = cliente.Telefono;
        clienteExistente.Direccion = cliente.Direccion;
        await _context.SaveChangesAsync();

        TempData["Mensaje"] = "Cliente actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var cliente = await _context.Clientes
            .Include(c => c.Prestamos)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.IdPersona == id);

        return cliente is null ? NotFound() : View(cliente);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [SesionAuthorize(RolesSistema.Administrador)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var cliente = await _context.Clientes
            .Include(c => c.Prestamos)
            .FirstOrDefaultAsync(c => c.IdPersona == id);

        if (cliente is null)
        {
            return RedirectToAction(nameof(Index));
        }

        cliente.Activo = false;
        await _context.SaveChangesAsync();

        TempData["Mensaje"] = "Cliente dado de baja correctamente.";
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

    private string? ObtenerReturnUrlLocal(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : null;
    }

    private bool EsAdministrador()
    {
        var rol = HttpContext.Session.GetString(SesionKeys.Rol);
        return string.Equals(rol, RolesSistema.Administrador, StringComparison.OrdinalIgnoreCase);
    }
}

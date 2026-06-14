using Biblioteca.Datos;
using Biblioteca.Filtros;
using Biblioteca.Modelos;
using Biblioteca.Seguridad;
using Biblioteca.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Controllers;

[SesionAuthorize]
public class PrestamosController : Controller
{
    private readonly BibliotecaContext _context;

    public PrestamosController(BibliotecaContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? vista, string? orden)
    {
        var hoy = DateTime.Today;
        var mostrarVencidos = string.Equals(vista, "vencidos", StringComparison.OrdinalIgnoreCase);
        var ordenarPorFecha = string.Equals(orden, "fecha", StringComparison.OrdinalIgnoreCase);

        IQueryable<Prestamo> prestamosQuery = _context.Prestamos
            .Include(p => p.Empleado)
            .Include(p => p.ItemsPrestamo)
            .ThenInclude(i => i.Libro);

        if (mostrarVencidos)
        {
            prestamosQuery = prestamosQuery.Where(p =>
                p.FechaDevolucion == null && p.FechaEstimadaDevolucion.Date < hoy);
        }

        prestamosQuery = ordenarPorFecha
            ? prestamosQuery.OrderByDescending(p => p.Fecha).ThenByDescending(p => p.IdPrestamo)
            : prestamosQuery.OrderBy(p => p.IdPrestamo).ThenBy(p => p.Fecha);

        var prestamos = await prestamosQuery
            .AsNoTracking()
            .ToListAsync();

        ViewBag.Vista = vista;
        ViewBag.Orden = orden;

        return View(prestamos);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var prestamo = await _context.Prestamos
            .Include(p => p.Empleado)
            .Include(p => p.ItemsPrestamo)
            .ThenInclude(i => i.Libro)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdPrestamo == id);

        return prestamo is null ? NotFound() : View(prestamo);
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    public async Task<IActionResult> Baja(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var prestamo = await _context.Prestamos
            .Include(p => p.Empleado)
            .Include(p => p.ItemsPrestamo)
            .ThenInclude(i => i.Libro)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdPrestamo == id);

        return prestamo is null ? NotFound() : View(prestamo);
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    [HttpPost, ActionName("Baja")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BajaConfirmed(int id)
    {
        var legajoAdministrador = ObtenerLegajoEmpleadoDesdeSesion();

        if (legajoAdministrador is null)
        {
            return RedirectToAction("Login", "Cuentas");
        }

        try
        {
            await DarDeBajaPrestamoAsync(id, legajoAdministrador.Value);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> Create()
    {
        var legajoEmpleado = ObtenerLegajoEmpleadoDesdeSesion();

        if (legajoEmpleado is null)
        {
            return RedirectToAction("Login", "Cuentas");
        }

        await CargarDatosPrestamoAsync(legajoEmpleado.Value);
        return View(new PrestamoCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PrestamoCreateViewModel prestamoModel)
    {
        var legajoEmpleado = ObtenerLegajoEmpleadoDesdeSesion();

        if (legajoEmpleado is null)
        {
            return RedirectToAction("Login", "Cuentas");
        }

        if (prestamoModel.LibrosIds.Count == 0)
        {
            ModelState.AddModelError(nameof(PrestamoCreateViewModel.LibrosIds), "Debe seleccionar al menos un libro.");
        }

        if (!ModelState.IsValid)
        {
            await CargarDatosPrestamoAsync(legajoEmpleado.Value, prestamoModel);
            return View(prestamoModel);
        }

        try
        {
            await RegistrarPrestamoAsync(
                legajoEmpleado.Value,
                prestamoModel.LibrosIds,
                prestamoModel.FechaPrestamo,
                prestamoModel.FechaEstimadaDevolucion);

            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await CargarDatosPrestamoAsync(legajoEmpleado.Value, prestamoModel);
            return View(prestamoModel);
        }
    }

    public async Task<IActionResult> Devolver(int? id)
    {
        if (id is null)
        {
            return View(new DevolucionPrestamoViewModel());
        }

        var model = await CrearModeloDevolucionAsync(id.Value);

        if (model is null)
        {
            ModelState.AddModelError(nameof(DevolucionPrestamoViewModel.IdPrestamo), "No se encontro un prestamo con ese ID.");
            return View(new DevolucionPrestamoViewModel { IdPrestamo = id.Value });
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Devolver(DevolucionPrestamoViewModel devolucionModel)
    {
        if (!ModelState.IsValid)
        {
            var model = await CrearModeloDevolucionAsync(devolucionModel.IdPrestamo, devolucionModel.FechaDevolucion);
            return View(model ?? devolucionModel);
        }

        try
        {
            var legajoEmpleado = ObtenerLegajoEmpleadoDesdeSesion();

            if (legajoEmpleado is null)
            {
                return RedirectToAction("Login", "Cuentas");
            }

            await RegistrarDevolucionAsync(devolucionModel.IdPrestamo, devolucionModel.FechaDevolucion, legajoEmpleado.Value);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var model = await CrearModeloDevolucionAsync(devolucionModel.IdPrestamo, devolucionModel.FechaDevolucion);
            return View(model ?? devolucionModel);
        }
    }

    private async Task<Prestamo> RegistrarPrestamoAsync(
        int legajoEmpleado,
        IEnumerable<int> librosIds,
        DateTime fechaPrestamo,
        DateTime fechaEstimadaDevolucion)
    {
        var ids = librosIds.ToList();

        if (ids.Count == 0)
        {
            throw new InvalidOperationException("El prestamo debe incluir al menos un libro.");
        }

        if (fechaEstimadaDevolucion.Date < fechaPrestamo.Date)
        {
            throw new InvalidOperationException("La fecha de devolucion no puede ser anterior a la fecha de prestamo.");
        }

        var empleadoExiste = await _context.Empleados.AnyAsync(e => e.Legajo == legajoEmpleado);

        if (!empleadoExiste)
        {
            throw new InvalidOperationException("El empleado indicado no existe.");
        }

        var cantidadesPorLibro = ids
            .GroupBy(id => id)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Count());

        var libros = await _context.Libros
            .Where(libro => cantidadesPorLibro.Keys.Contains(libro.IdLibro))
            .ToListAsync();

        if (libros.Count != cantidadesPorLibro.Count)
        {
            throw new InvalidOperationException("Uno o mas libros indicados no existen.");
        }

        foreach (var libro in libros)
        {
            var cantidadSolicitada = cantidadesPorLibro[libro.IdLibro];

            if (!libro.Activo)
            {
                throw new InvalidOperationException($"El libro '{libro.Titulo}' no esta activo.");
            }

            if (libro.StockDisponible < cantidadSolicitada)
            {
                throw new InvalidOperationException($"El libro '{libro.Titulo}' no tiene stock disponible suficiente.");
            }
        }

        var prestamo = new Prestamo
        {
            LegajoEmpleado = legajoEmpleado,
            Fecha = fechaPrestamo,
            FechaEstimadaDevolucion = fechaEstimadaDevolucion,
            Estado = "Activo"
        };

        foreach (var idLibro in ids)
        {
            prestamo.ItemsPrestamo.Add(new ItemPrestamo
            {
                IdLibro = idLibro,
                EstadoItem = EstadoItemPrestamo.Prestado
            });
        }

        _context.Prestamos.Add(prestamo);

        foreach (var libro in libros)
        {
            var cantidad = cantidadesPorLibro[libro.IdLibro];
            libro.StockDisponible -= cantidad;

            _context.MovimientosStock.Add(new MovimientoStock
            {
                IdLibro = libro.IdLibro,
                LegajoEmpleado = legajoEmpleado,
                TipoMovimiento = TipoMovimientoStock.Prestado,
                Cantidad = cantidad,
                Motivo = "Prestamo registrado",
                Fecha = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();

        return prestamo;
    }

    private async Task RegistrarDevolucionAsync(int idPrestamo, DateTime fechaDevolucion, int legajoEmpleado)
    {
        var prestamo = await _context.Prestamos
            .Include(p => p.ItemsPrestamo)
            .ThenInclude(i => i.Libro)
            .FirstOrDefaultAsync(p => p.IdPrestamo == idPrestamo);

        if (prestamo is null)
        {
            throw new InvalidOperationException("El prestamo indicado no existe.");
        }

        var itemsPendientes = prestamo.ItemsPrestamo
            .Where(i => i.FechaDevolucion is null)
            .ToList();

        if (itemsPendientes.Count == 0)
        {
            throw new InvalidOperationException("El prestamo ya fue devuelto.");
        }

        if (fechaDevolucion.Date < prestamo.Fecha.Date)
        {
            throw new InvalidOperationException("La fecha de devolucion no puede ser anterior a la fecha de prestamo.");
        }

        var diasRetraso = Math.Max(0, (fechaDevolucion.Date - prestamo.FechaEstimadaDevolucion.Date).Days);

        foreach (var item in itemsPendientes)
        {
            item.FechaDevolucion = fechaDevolucion;
            item.DiasRetraso = diasRetraso;
            item.EstadoItem = diasRetraso > 0 ? EstadoItemPrestamo.Vencido : EstadoItemPrestamo.Devuelto;
            item.Libro.StockDisponible += 1;

            _context.MovimientosStock.Add(new MovimientoStock
            {
                IdLibro = item.IdLibro,
                LegajoEmpleado = legajoEmpleado,
                TipoMovimiento = TipoMovimientoStock.AltaStock,
                Cantidad = 1,
                Motivo = "Devolucion de prestamo",
                Fecha = DateTime.Now
            });
        }

        prestamo.FechaDevolucion = fechaDevolucion;
        prestamo.Estado = "Finalizado";

        await _context.SaveChangesAsync();
    }

    private async Task DarDeBajaPrestamoAsync(int idPrestamo, int legajoAdministrador)
    {
        var prestamo = await _context.Prestamos
            .Include(p => p.ItemsPrestamo)
            .ThenInclude(i => i.Libro)
            .FirstOrDefaultAsync(p => p.IdPrestamo == idPrestamo);

        if (prestamo is null)
        {
            throw new InvalidOperationException("El prestamo indicado no existe.");
        }

        var pendientesPorLibro = prestamo.ItemsPrestamo
            .Where(i => i.FechaDevolucion is null)
            .GroupBy(i => i.Libro)
            .ToList();

        foreach (var grupo in pendientesPorLibro)
        {
            var libro = grupo.Key;
            var cantidad = grupo.Count();

            libro.StockDisponible += cantidad;

            _context.MovimientosStock.Add(new MovimientoStock
            {
                IdLibro = libro.IdLibro,
                LegajoEmpleado = legajoAdministrador,
                TipoMovimiento = TipoMovimientoStock.AltaStock,
                Cantidad = cantidad,
                Motivo = "Restitución de stock por baja de prestamo por administrador",
                Fecha = DateTime.Now
            });
        }

        _context.Prestamos.Remove(prestamo);
        await _context.SaveChangesAsync();
    }

    private async Task<DevolucionPrestamoViewModel?> CrearModeloDevolucionAsync(
        int idPrestamo,
        DateTime? fechaDevolucion = null)
    {
        var prestamo = await _context.Prestamos
            .Include(p => p.Empleado)
            .Include(p => p.ItemsPrestamo)
            .ThenInclude(i => i.Libro)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdPrestamo == idPrestamo);

        if (prestamo is null)
        {
            return null;
        }

        var itemsPendientes = prestamo.ItemsPrestamo
            .Where(i => i.FechaDevolucion is null)
            .OrderBy(i => i.Libro.Titulo)
            .ToList();
        var estaVencido = itemsPendientes.Count > 0
            && prestamo.FechaEstimadaDevolucion.Date < DateTime.Today;

        return new DevolucionPrestamoViewModel
        {
            IdPrestamo = prestamo.IdPrestamo,
            Prestamo = $"Prestamo #{prestamo.IdPrestamo}",
            Empleado = prestamo.Empleado.NombreCompleto,
            Libros = itemsPendientes.Count == 0
                ? "Sin libros pendientes de devolucion"
                : string.Join(", ", itemsPendientes.Select(i => i.Libro.Titulo)),
            FechaPrestamo = prestamo.Fecha,
            FechaEstimadaDevolucion = prestamo.FechaEstimadaDevolucion,
            FechaDevolucion = fechaDevolucion ?? DateTime.Today,
            EstaVencido = estaVencido,
            DiasVencido = estaVencido ? (DateTime.Today - prestamo.FechaEstimadaDevolucion.Date).Days : 0,
            YaDevuelto = itemsPendientes.Count == 0
        };
    }

    private int? ObtenerLegajoEmpleadoDesdeSesion()
    {
        var legajoTexto = HttpContext.Session.GetString(SesionKeys.LegajoEmpleado);

        return int.TryParse(legajoTexto, out var legajoEmpleado)
            ? legajoEmpleado
            : null;
    }

    private async Task CargarDatosPrestamoAsync(int legajoEmpleado, PrestamoCreateViewModel? model = null)
    {
        var empleado = await _context.Empleados
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Legajo == legajoEmpleado);

        ViewBag.EmpleadoLogueado = empleado is null
            ? "Empleado no identificado"
            : empleado.NombreCompleto;

        var libros = await _context.Libros
            .Where(l => l.Activo && l.StockDisponible > 0)
            .OrderBy(l => l.Titulo)
            .Select(l => new
            {
                l.IdLibro,
                Descripcion = $"{l.Titulo} - Disponible: {l.StockDisponible}"
            })
            .ToListAsync();

        ViewBag.Libros = new MultiSelectList(libros, "IdLibro", "Descripcion", model?.LibrosIds);
    }
}

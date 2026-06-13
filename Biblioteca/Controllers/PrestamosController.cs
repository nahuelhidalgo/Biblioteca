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

    public async Task<IActionResult> Index()
    {
        var prestamos = await _context.Prestamos
            .Include(p => p.Empleado)
            .Include(p => p.ItemsPrestamo)
            .ThenInclude(i => i.Libro)
            .OrderByDescending(p => p.Fecha)
            .AsNoTracking()
            .ToListAsync();

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

    [SesionAuthorize(RolesSistema.Administrador)]
    public async Task<IActionResult> Devolver(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var item = await _context.ItemsPrestamo
            .Include(i => i.Libro)
            .Include(i => i.Prestamo)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.IdItemPrestamo == id);

        if (item is null)
        {
            return NotFound();
        }

        var model = new DevolucionPrestamoViewModel
        {
            IdItemPrestamo = item.IdItemPrestamo,
            Prestamo = $"Prestamo #{item.IdPrestamo}",
            Libro = item.Libro.Titulo,
            FechaPrestamo = item.Prestamo.Fecha,
            FechaEstimadaDevolucion = item.Prestamo.FechaEstimadaDevolucion,
            FechaDevolucion = DateTime.Today
        };

        return View(model);
    }

    [SesionAuthorize(RolesSistema.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Devolver(DevolucionPrestamoViewModel devolucionModel)
    {
        if (!ModelState.IsValid)
        {
            return View(devolucionModel);
        }

        try
        {
            await RegistrarDevolucionAsync(devolucionModel.IdItemPrestamo, devolucionModel.FechaDevolucion);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(devolucionModel);
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
            throw new InvalidOperationException("La fecha estimada de devolucion no puede ser anterior a la fecha de prestamo.");
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
                TipoMovimiento = TipoMovimientoStock.Prestado,
                Cantidad = cantidad,
                Motivo = "Prestamo registrado",
                Fecha = fechaPrestamo
            });
        }

        await _context.SaveChangesAsync();

        return prestamo;
    }

    private async Task RegistrarDevolucionAsync(int idItemPrestamo, DateTime fechaDevolucion)
    {
        var item = await _context.ItemsPrestamo
            .Include(i => i.Libro)
            .Include(i => i.Prestamo)
            .FirstOrDefaultAsync(i => i.IdItemPrestamo == idItemPrestamo);

        if (item is null)
        {
            throw new InvalidOperationException("El item de prestamo indicado no existe.");
        }

        if (item.FechaDevolucion is not null)
        {
            throw new InvalidOperationException("El item de prestamo ya fue devuelto.");
        }

        if (fechaDevolucion.Date < item.Prestamo.Fecha.Date)
        {
            throw new InvalidOperationException("La fecha de devolucion no puede ser anterior a la fecha de prestamo.");
        }

        var diasRetraso = Math.Max(0, (fechaDevolucion.Date - item.Prestamo.FechaEstimadaDevolucion.Date).Days);

        item.FechaDevolucion = fechaDevolucion;
        item.DiasRetraso = diasRetraso;
        item.EstadoItem = diasRetraso > 0 ? EstadoItemPrestamo.Vencido : EstadoItemPrestamo.Devuelto;
        item.Libro.StockDisponible += 1;

        _context.MovimientosStock.Add(new MovimientoStock
        {
            IdLibro = item.IdLibro,
            TipoMovimiento = TipoMovimientoStock.AltaStock,
            Cantidad = 1,
            Motivo = "Devolucion de prestamo",
            Fecha = fechaDevolucion
        });

        var quedanItemsPendientes = await _context.ItemsPrestamo
            .AnyAsync(i => i.IdPrestamo == item.IdPrestamo
                && i.IdItemPrestamo != item.IdItemPrestamo
                && i.FechaDevolucion == null);

        if (!quedanItemsPendientes)
        {
            item.Prestamo.FechaDevolucion = fechaDevolucion;
            item.Prestamo.Estado = "Finalizado";
        }

        await _context.SaveChangesAsync();
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
            : $"{empleado.Apellido}, {empleado.Nombre} ({empleado.Legajo})";

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

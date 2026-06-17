using Biblioteca.Datos;
using Biblioteca.Filtros;
using Biblioteca.Modelos;
using Biblioteca.Seguridad;
using Biblioteca.ViewModels;
using Microsoft.AspNetCore.Mvc;
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
        var ahora = DateTime.Now;
        var mostrarVencidos = string.Equals(vista, "vencidos", StringComparison.OrdinalIgnoreCase);
        var ordenarPorFecha = string.Equals(orden, "fecha", StringComparison.OrdinalIgnoreCase);
        var legajoEmpleado = ObtenerLegajoEmpleadoDesdeSesion();
        var esAdministrador = EsAdministrador();

        IQueryable<Prestamo> prestamosQuery = _context.Prestamos
            .Include(p => p.Empleado)
            .Include(p => p.ItemsPrestamo)
            .ThenInclude(i => i.Libro);

        if (!esAdministrador && legajoEmpleado is not null)
        {
            prestamosQuery = prestamosQuery.Where(p => p.LegajoEmpleado == legajoEmpleado.Value);
        }
        else if (!esAdministrador)
        {
            prestamosQuery = prestamosQuery.Where(p => false);
        }

        if (mostrarVencidos)
        {
            prestamosQuery = prestamosQuery.Where(p =>
                p.FechaDevolucion == null && p.FechaEstimadaDevolucion < ahora);
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

        if (prestamo is not null && !PuedeAccederPrestamo(prestamo.LegajoEmpleado))
        {
            return NotFound();
        }

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

        await RefrescarCarritoAsync(prestamoModel);

        if (prestamoModel.Accion?.StartsWith("quitar:", StringComparison.OrdinalIgnoreCase) == true)
        {
            var idTexto = prestamoModel.Accion["quitar:".Length..];

            if (int.TryParse(idTexto, out var idLibro))
            {
                prestamoModel.Items.RemoveAll(i => i.IdLibro == idLibro);
            }

            ModelState.Clear();
            prestamoModel.Accion = null;
            return View(prestamoModel);
        }

        if (string.Equals(prestamoModel.Accion, "agregar", StringComparison.OrdinalIgnoreCase))
        {
            await AgregarLibroAlCarritoAsync(prestamoModel);

            if (!ModelState.IsValid)
            {
                return View(prestamoModel);
            }

            ModelState.Clear();
            prestamoModel.ISBN = string.Empty;
            prestamoModel.Cantidad = 1;
            prestamoModel.Accion = null;
            return View(prestamoModel);
        }

        ModelState.Remove(nameof(PrestamoCreateViewModel.ISBN));
        ModelState.Remove(nameof(PrestamoCreateViewModel.Cantidad));
        ModelState.Remove(nameof(PrestamoCreateViewModel.Accion));

        if (prestamoModel.Items.Count == 0)
        {
            ModelState.AddModelError(nameof(PrestamoCreateViewModel.Items), "Debe agregar al menos un libro al prestamo.");
        }

        await ValidarCarritoAsync(prestamoModel);

        if (!ModelState.IsValid)
        {
            return View(prestamoModel);
        }

        try
        {
            var fechaPrestamo = DateTime.Now;
            var librosIds = prestamoModel.Items
                .SelectMany(item => Enumerable.Repeat(item.IdLibro, item.Cantidad))
                .ToList();

            await RegistrarPrestamoAsync(
                legajoEmpleado.Value,
                librosIds,
                fechaPrestamo,
                prestamoModel.FechaEstimadaDevolucion);

            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
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

            await RegistrarDevolucionAsync(devolucionModel.IdPrestamo, DateTime.Now, legajoEmpleado.Value);
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

        if (fechaEstimadaDevolucion < fechaPrestamo)
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

        await using var transaction = await _context.Database.BeginTransactionAsync();

        _context.Prestamos.Add(prestamo);

        foreach (var libro in libros)
        {
            var cantidad = cantidadesPorLibro[libro.IdLibro];
            libro.StockDisponible -= cantidad;
        }

        await _context.SaveChangesAsync();

        foreach (var libro in libros)
        {
            var cantidad = cantidadesPorLibro[libro.IdLibro];
            _context.MovimientosStock.Add(new MovimientoStock
            {
                IdLibro = libro.IdLibro,
                IdPrestamo = prestamo.IdPrestamo,
                LegajoEmpleado = legajoEmpleado,
                TipoMovimiento = TipoMovimientoStock.Prestado,
                Cantidad = cantidad,
                Motivo = "Prestamo registrado",
                Fecha = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return prestamo;
    }

    private async Task AgregarLibroAlCarritoAsync(PrestamoCreateViewModel prestamoModel)
    {
        var isbn = prestamoModel.ISBN.Trim();

        if (string.IsNullOrWhiteSpace(isbn))
        {
            ModelState.AddModelError(nameof(PrestamoCreateViewModel.ISBN), "Ingrese el ISBN del libro.");
            return;
        }

        if (prestamoModel.Cantidad <= 0)
        {
            ModelState.AddModelError(nameof(PrestamoCreateViewModel.Cantidad), "La cantidad debe ser mayor a cero.");
            return;
        }

        var libro = await _context.Libros
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.ISBN == isbn);

        if (libro is null)
        {
            ModelState.AddModelError(nameof(PrestamoCreateViewModel.ISBN), "No existe un libro con ese ISBN.");
            return;
        }

        if (!libro.Activo)
        {
            ModelState.AddModelError(nameof(PrestamoCreateViewModel.ISBN), "El libro encontrado no esta activo.");
            return;
        }

        var itemExistente = prestamoModel.Items.FirstOrDefault(i => i.IdLibro == libro.IdLibro);
        var cantidadActual = itemExistente?.Cantidad ?? 0;
        var cantidadTotal = cantidadActual + prestamoModel.Cantidad;

        if (libro.StockDisponible < cantidadTotal)
        {
            ModelState.AddModelError(
                nameof(PrestamoCreateViewModel.Cantidad),
                $"Stock insuficiente. Disponible: {libro.StockDisponible}; en carrito: {cantidadActual}.");
            return;
        }

        if (itemExistente is null)
        {
            prestamoModel.Items.Add(new PrestamoCarritoItemViewModel
            {
                IdLibro = libro.IdLibro,
                ISBN = libro.ISBN,
                Titulo = libro.Titulo,
                StockDisponible = libro.StockDisponible,
                Cantidad = prestamoModel.Cantidad
            });
        }
        else
        {
            itemExistente.ISBN = libro.ISBN;
            itemExistente.Titulo = libro.Titulo;
            itemExistente.StockDisponible = libro.StockDisponible;
            itemExistente.Cantidad = cantidadTotal;
        }
    }

    private async Task ValidarCarritoAsync(PrestamoCreateViewModel prestamoModel)
    {
        var ids = prestamoModel.Items.Select(i => i.IdLibro).Distinct().ToList();
        var libros = await _context.Libros
            .Where(l => ids.Contains(l.IdLibro))
            .AsNoTracking()
            .ToDictionaryAsync(l => l.IdLibro);

        foreach (var item in prestamoModel.Items)
        {
            if (!libros.TryGetValue(item.IdLibro, out var libro))
            {
                ModelState.AddModelError(string.Empty, $"El libro '{item.Titulo}' ya no existe.");
                continue;
            }

            if (!libro.Activo)
            {
                ModelState.AddModelError(string.Empty, $"El libro '{libro.Titulo}' no esta activo.");
            }

            if (item.Cantidad <= 0)
            {
                ModelState.AddModelError(string.Empty, $"La cantidad de '{libro.Titulo}' debe ser mayor a cero.");
            }

            if (item.Cantidad > libro.StockDisponible)
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"Stock insuficiente para '{libro.Titulo}'. Disponible: {libro.StockDisponible}.");
            }
        }
    }

    private async Task RefrescarCarritoAsync(PrestamoCreateViewModel prestamoModel)
    {
        prestamoModel.Items = prestamoModel.Items
            .Where(i => i.IdLibro > 0 && i.Cantidad > 0)
            .GroupBy(i => i.IdLibro)
            .Select(g => new PrestamoCarritoItemViewModel
            {
                IdLibro = g.Key,
                Cantidad = g.Sum(i => i.Cantidad),
                ISBN = g.First().ISBN,
                Titulo = g.First().Titulo,
                StockDisponible = g.First().StockDisponible
            })
            .ToList();

        if (prestamoModel.Items.Count == 0)
        {
            return;
        }

        var ids = prestamoModel.Items.Select(i => i.IdLibro).ToList();
        var libros = await _context.Libros
            .Where(l => ids.Contains(l.IdLibro))
            .AsNoTracking()
            .ToDictionaryAsync(l => l.IdLibro);

        foreach (var item in prestamoModel.Items)
        {
            if (!libros.TryGetValue(item.IdLibro, out var libro))
            {
                continue;
            }

            item.ISBN = libro.ISBN;
            item.Titulo = libro.Titulo;
            item.StockDisponible = libro.StockDisponible;
        }
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

        if (!PuedeAccederPrestamo(prestamo.LegajoEmpleado))
        {
            throw new InvalidOperationException("No tiene permisos para operar este prestamo.");
        }

        var itemsPendientes = prestamo.ItemsPrestamo
            .Where(i => i.FechaDevolucion is null)
            .ToList();

        if (itemsPendientes.Count == 0)
        {
            throw new InvalidOperationException("El prestamo ya fue devuelto.");
        }

        if (fechaDevolucion < prestamo.Fecha)
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
                IdPrestamo = prestamo.IdPrestamo,
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
                IdPrestamo = prestamo.IdPrestamo,
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

        if (!PuedeAccederPrestamo(prestamo.LegajoEmpleado))
        {
            return null;
        }

        var itemsPendientes = prestamo.ItemsPrestamo
            .Where(i => i.FechaDevolucion is null)
            .OrderBy(i => i.Libro.Titulo)
            .ToList();
        var ahora = DateTime.Now;
        var estaVencido = itemsPendientes.Count > 0
            && prestamo.FechaEstimadaDevolucion < ahora;

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
            FechaDevolucion = fechaDevolucion ?? ahora,
            EstaVencido = estaVencido,
            DiasVencido = estaVencido ? (ahora.Date - prestamo.FechaEstimadaDevolucion.Date).Days : 0,
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

    private bool PuedeAccederPrestamo(int legajoEmpleadoPrestamo)
    {
        if (EsAdministrador())
        {
            return true;
        }

        var legajoEmpleado = ObtenerLegajoEmpleadoDesdeSesion();

        return legajoEmpleado == legajoEmpleadoPrestamo;
    }

    private bool EsAdministrador()
    {
        var rol = HttpContext.Session.GetString(SesionKeys.Rol);

        return string.Equals(rol, RolesSistema.Administrador, StringComparison.OrdinalIgnoreCase);
    }
}

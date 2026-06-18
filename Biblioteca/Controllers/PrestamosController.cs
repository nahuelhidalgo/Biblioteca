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
    private const string ErrorAgregarLibro = "AgregarLibro";
    private readonly BibliotecaContext _context;

    public PrestamosController(BibliotecaContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(
        string? vista,
        string? orden,
        int? idCliente,
        bool mostrarFinalizados = false)
    {
        var ahora = DateTime.Now;
        var mostrarVencidos = string.Equals(vista, "vencidos", StringComparison.OrdinalIgnoreCase);
        var criterioOrden = orden?.ToLowerInvariant() switch
        {
            "activos" => "activos",
            "id" => "id",
            _ => "fecha"
        };

        IQueryable<Prestamo> prestamosQuery = _context.Prestamos
            .Include(p => p.EmpleadoRegistro)
            .Include(p => p.Cliente)
            .Include(p => p.ItemsPrestamo)
            .ThenInclude(i => i.Libro);

        if (idCliente.HasValue)
        {
            prestamosQuery = prestamosQuery.Where(p => p.IdCliente == idCliente.Value);
        }

        if (!mostrarFinalizados)
        {
            prestamosQuery = prestamosQuery.Where(p => p.Estado != "Finalizado");
        }

        if (mostrarVencidos)
        {
            prestamosQuery = prestamosQuery.Where(p =>
                p.FechaDevolucion == null && p.FechaEstimadaDevolucion < ahora);
        }

        prestamosQuery = criterioOrden switch
        {
            "activos" => prestamosQuery
                .OrderBy(p => p.Estado == "Activo" ? 0 : 1)
                .ThenBy(p => p.IdPrestamo),
            "id" => prestamosQuery.OrderBy(p => p.IdPrestamo),
            _ => prestamosQuery
                .OrderByDescending(p => p.Fecha)
                .ThenByDescending(p => p.IdPrestamo)
        };

        var clientes = await _context.Clientes
            .OrderBy(c => c.Apellido)
            .ThenBy(c => c.Nombre)
            .Select(c => new
            {
                c.IdPersona,
                Descripcion = $"{c.Nombre} {c.Apellido} - DNI {c.DNI}"
            })
            .AsNoTracking()
            .ToListAsync();

        var prestamos = await prestamosQuery
            .AsNoTracking()
            .ToListAsync();

        ViewBag.Vista = vista;
        ViewBag.Orden = criterioOrden;
        ViewBag.IdCliente = idCliente;
        ViewBag.MostrarFinalizados = mostrarFinalizados;
        ViewBag.Clientes = new SelectList(clientes, "IdPersona", "Descripcion", idCliente);

        return View(prestamos);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var prestamo = await _context.Prestamos
            .Include(p => p.EmpleadoRegistro)
            .Include(p => p.Cliente)
            .Include(p => p.ItemsPrestamo)
            .ThenInclude(i => i.Libro)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdPrestamo == id);

        if (prestamo is not null && !PuedeAccederPrestamo(prestamo.IdEmpleadoRegistro))
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
            .Include(p => p.EmpleadoRegistro)
            .Include(p => p.Cliente)
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
        var idAdministrador = ObtenerIdEmpleadoDesdeSesion();

        if (idAdministrador is null)
        {
            return RedirectToAction("Login", "Cuentas");
        }

        try
        {
            await DarDeBajaPrestamoAsync(id, idAdministrador.Value);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> Create(int? idCliente = null)
    {
        var idEmpleado = ObtenerIdEmpleadoDesdeSesion();

        if (idEmpleado is null)
        {
            return RedirectToAction("Login", "Cuentas");
        }

        var model = new PrestamoCreateViewModel { IdCliente = idCliente };
        await CargarDatosPrestamoAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarAlCarrito(PrestamoCreateViewModel prestamoModel)
    {
        var idEmpleado = ObtenerIdEmpleadoDesdeSesion();

        if (idEmpleado is null)
        {
            return RedirectToAction("Login", "Cuentas");
        }

        await RefrescarCarritoAsync(prestamoModel);
        ModelState.Clear();
        await AgregarLibroAlCarritoAsync(prestamoModel);

        if (!ModelState.IsValid)
        {
            await CargarDatosPrestamoAsync(prestamoModel);
            return View("Create", prestamoModel);
        }

        ModelState.Clear();
        prestamoModel.ISBN = null;
        prestamoModel.Cantidad = 1;
        await CargarDatosPrestamoAsync(prestamoModel);
        return View("Create", prestamoModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuitarDelCarrito(PrestamoCreateViewModel prestamoModel, int idLibro)
    {
        if (ObtenerIdEmpleadoDesdeSesion() is null)
        {
            return RedirectToAction("Login", "Cuentas");
        }

        await RefrescarCarritoAsync(prestamoModel);
        var item = prestamoModel.Items.FirstOrDefault(i => i.IdLibro == idLibro);

        if (item is not null)
        {
            if (item.Cantidad > 1)
            {
                item.Cantidad -= 1;
            }
            else
            {
                prestamoModel.Items.Remove(item);
            }
        }
        ModelState.Clear();
        prestamoModel.ISBN = null;
        prestamoModel.Cantidad = 1;
        await CargarDatosPrestamoAsync(prestamoModel);

        return View("Create", prestamoModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PrestamoCreateViewModel prestamoModel)
    {
        var idEmpleado = ObtenerIdEmpleadoDesdeSesion();

        if (idEmpleado is null)
        {
            return RedirectToAction("Login", "Cuentas");
        }

        await RefrescarCarritoAsync(prestamoModel);

        ModelState.Remove(nameof(PrestamoCreateViewModel.ISBN));
        ModelState.Remove(nameof(PrestamoCreateViewModel.Cantidad));

        if (prestamoModel.Items.Count == 0)
        {
            ModelState.AddModelError(nameof(PrestamoCreateViewModel.Items), "Debe agregar al menos un libro al prestamo.");
        }

        await ValidarCarritoAsync(prestamoModel);

        if (!ModelState.IsValid)
        {
            await CargarDatosPrestamoAsync(prestamoModel);
            return View(prestamoModel);
        }

        try
        {
            var fechaPrestamo = DateTime.Now;
            var fechaDevolucion = fechaPrestamo.Date.AddDays(prestamoModel.DuracionDias!.Value);
            var librosIds = prestamoModel.Items
                .SelectMany(item => Enumerable.Repeat(item.IdLibro, item.Cantidad))
                .ToList();

            await RegistrarPrestamoAsync(
                idEmpleado.Value,
                prestamoModel.IdCliente!.Value,
                librosIds,
                fechaPrestamo,
                fechaDevolucion);

            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await CargarDatosPrestamoAsync(prestamoModel);
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
            var idEmpleado = ObtenerIdEmpleadoDesdeSesion();

            if (idEmpleado is null)
            {
                return RedirectToAction("Login", "Cuentas");
            }

            await RegistrarDevolucionAsync(devolucionModel.IdPrestamo, DateTime.Now, idEmpleado.Value);
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
        int idEmpleado,
        int idCliente,
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

        var empleadoExiste = await _context.Empleados.AnyAsync(e => e.IdPersona == idEmpleado);

        if (!empleadoExiste)
        {
            throw new InvalidOperationException("El empleado indicado no existe.");
        }

        var clienteExiste = await _context.Clientes.AnyAsync(c =>
            c.IdPersona == idCliente && c.Activo);

        if (!clienteExiste)
        {
            throw new InvalidOperationException("El cliente indicado no existe o se encuentra inactivo.");
        }

        var tienePrestamoVencido = await _context.Prestamos.AnyAsync(p =>
            p.IdCliente == idCliente
            && p.FechaDevolucion == null
            && p.FechaEstimadaDevolucion < fechaPrestamo);

        if (tienePrestamoVencido)
        {
            throw new InvalidOperationException(
                "El usuario tiene un préstamo vencido. Debe registrar su devolución antes de crear un nuevo préstamo.");
        }

        var prestamosActivosDelCliente = await _context.Prestamos.CountAsync(p =>
            p.IdCliente == idCliente && p.FechaDevolucion == null);

        if (prestamosActivosDelCliente >= 2)
        {
            throw new InvalidOperationException(
                "El usuario cuenta con 2 préstamos activos. Para crear un nuevo préstamo debe devolver alguno de los anteriores.");
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
            IdEmpleadoRegistro = idEmpleado,
            IdCliente = idCliente,
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
                IdEmpleado = idEmpleado,
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
        var isbn = prestamoModel.ISBN?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(isbn))
        {
            ModelState.AddModelError(ErrorAgregarLibro, "Ingrese el ISBN del libro.");
            return;
        }

        if (prestamoModel.Cantidad is null or <= 0)
        {
            ModelState.AddModelError(ErrorAgregarLibro, "La cantidad debe ser mayor a cero.");
            return;
        }

        var libro = await _context.Libros
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.ISBN == isbn);

        if (libro is null)
        {
            ModelState.AddModelError(ErrorAgregarLibro, "No existe un libro con ese ISBN.");
            return;
        }

        if (!libro.Activo)
        {
            ModelState.AddModelError(ErrorAgregarLibro, "El libro encontrado no está activo.");
            return;
        }

        var itemExistente = prestamoModel.Items.FirstOrDefault(i => i.IdLibro == libro.IdLibro);
        var cantidadActual = itemExistente?.Cantidad ?? 0;
        var cantidadAgregar = prestamoModel.Cantidad.Value;
        var cantidadTotal = cantidadActual + cantidadAgregar;

        if (libro.StockDisponible < cantidadTotal)
        {
            ModelState.AddModelError(
                ErrorAgregarLibro,
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
                Cantidad = cantidadAgregar
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

    private async Task CargarDatosPrestamoAsync(PrestamoCreateViewModel model)
    {
        var libros = await _context.Libros
            .Where(l => l.Activo)
            .OrderBy(l => l.ISBN)
            .Select(l => new
            {
                l.ISBN,
                Descripcion = $"{l.ISBN} - {l.Titulo} - {l.Editorial.Nombre}"
            })
            .AsNoTracking()
            .ToListAsync();

        var clientes = await _context.Clientes
            .Where(c => c.Activo)
            .OrderBy(c => c.Apellido)
            .ThenBy(c => c.Nombre)
            .Select(c => new
            {
                c.IdPersona,
                Descripcion = $"{c.Nombre} {c.Apellido} - DNI {c.DNI}"
            })
            .AsNoTracking()
            .ToListAsync();

        ViewBag.LibrosISBN = new SelectList(libros, "ISBN", "Descripcion", model.ISBN);
        ViewBag.Clientes = new SelectList(clientes, "IdPersona", "Descripcion", model.IdCliente);
    }

    private async Task RegistrarDevolucionAsync(int idPrestamo, DateTime fechaDevolucion, int idEmpleado)
    {
        var prestamo = await _context.Prestamos
            .Include(p => p.ItemsPrestamo)
            .ThenInclude(i => i.Libro)
            .FirstOrDefaultAsync(p => p.IdPrestamo == idPrestamo);

        if (prestamo is null)
        {
            throw new InvalidOperationException("El prestamo indicado no existe.");
        }

        if (!PuedeAccederPrestamo(prestamo.IdEmpleadoRegistro))
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
                IdEmpleado = idEmpleado,
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

    private async Task DarDeBajaPrestamoAsync(int idPrestamo, int idAdministrador)
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
                IdEmpleado = idAdministrador,
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
            .Include(p => p.EmpleadoRegistro)
            .Include(p => p.Cliente)
            .Include(p => p.ItemsPrestamo)
            .ThenInclude(i => i.Libro)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdPrestamo == idPrestamo);

        if (prestamo is null)
        {
            return null;
        }

        if (!PuedeAccederPrestamo(prestamo.IdEmpleadoRegistro))
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
            Cliente = prestamo.Cliente.NombreCompleto,
            EmpleadoRegistro = prestamo.EmpleadoRegistro.NombreCompleto,
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

    private int? ObtenerIdEmpleadoDesdeSesion()
    {
        var idTexto = HttpContext.Session.GetString(SesionKeys.IdEmpleado);

        return int.TryParse(idTexto, out var idEmpleado)
            ? idEmpleado
            : null;
    }

    private bool PuedeAccederPrestamo(int idEmpleadoRegistro)
    {
        if (EsAdministrador())
        {
            return true;
        }

        var idEmpleado = ObtenerIdEmpleadoDesdeSesion();

        return idEmpleado == idEmpleadoRegistro;
    }

    private bool EsAdministrador()
    {
        var rol = HttpContext.Session.GetString(SesionKeys.Rol);

        return string.Equals(rol, RolesSistema.Administrador, StringComparison.OrdinalIgnoreCase);
    }
}

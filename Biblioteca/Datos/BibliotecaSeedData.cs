using Biblioteca.Modelos;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Datos;

public static class BibliotecaSeedData
{
    public static async Task InicializarAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BibliotecaContext>();

        await context.Database.MigrateAsync();

        var personas = await CrearPersonasYUsuariosAsync(context);
        var libros = await CrearCatalogoAsync(context);

        await CrearStockInicialAsync(context, libros);
        await CrearPrestamosAsync(context, personas.Empleados, personas.Clientes, libros);
    }

    private static async Task<(Dictionary<string, Empleado> Empleados, Dictionary<string, Cliente> Clientes)>
        CrearPersonasYUsuariosAsync(BibliotecaContext context)
    {
        var juan = await ObtenerOCrearEmpleadoAsync(
            context, "30123456", "Juan", "Perez", "1123456789", "Sucursal Central");
        var carlos = await ObtenerOCrearEmpleadoAsync(
            context, "30345678", "Carlos", "Gomez", "1145678901", "Administracion");

        var ana = await ObtenerOCrearClienteAsync(
            context, "30234567", "Ana Maria", "Perez", "1134567890", "ana.perez@email.com", "Buenos Aires 1200");
        var lucia = await ObtenerOCrearClienteAsync(
            context, "30456789", "Lucia", "Fernandez", "1156789012", "lucia.fernandez@email.com", "Cordoba 850");
        var martin = await ObtenerOCrearClienteAsync(
            context, "30567890", "Martin", "Silva", "1167890123", "martin.silva@email.com", "Santa Fe 430");

        await context.SaveChangesAsync();

        await ObtenerOCrearUsuarioAsync(
            context, juan.IdPersona, "juan.perez@ort.edu.ar", "Operador1", "Operador");
        await ObtenerOCrearUsuarioAsync(
            context, carlos.IdPersona, "carlos.gomez@ort.edu.ar", "Administrador1", "Administrador");

        await context.SaveChangesAsync();

        return (
            new Dictionary<string, Empleado>
            {
                ["Juan Perez"] = juan,
                ["Carlos Gomez"] = carlos
            },
            new Dictionary<string, Cliente>
            {
                ["Ana Maria Perez"] = ana,
                ["Lucia Fernandez"] = lucia,
                ["Martin Silva"] = martin
            });
    }

    private static async Task<Dictionary<string, Libro>> CrearCatalogoAsync(BibliotecaContext context)
    {
        var garciaMarquez = await ObtenerOCrearAutorAsync(context, "Gabriel", "Garcia Marquez", "Colombia");
        var cortazar = await ObtenerOCrearAutorAsync(context, "Julio", "Cortazar", "Argentina");
        var allende = await ObtenerOCrearAutorAsync(context, "Isabel", "Allende", "Chile");
        var sabato = await ObtenerOCrearAutorAsync(context, "Ernesto", "Sabato", "Argentina");

        var novela = await ObtenerOCrearCategoriaAsync(context, "Novela", "Ficcion narrativa.");
        var historia = await ObtenerOCrearCategoriaAsync(context, "Historia", "Libros de historia y divulgacion historica.");

        var planeta = await ObtenerOCrearEditorialAsync(context, "Planeta", "Grupo editorial Planeta.");
        var penguin = await ObtenerOCrearEditorialAsync(context, "Penguin Random House", "Editorial internacional.");
        var sigloXxi = await ObtenerOCrearEditorialAsync(context, "Siglo XXI", "Publicaciones academicas.");

        await context.SaveChangesAsync();

        var cienAnios = await ObtenerOCrearLibroAsync(context, "9780307474728", "Cien anios de soledad", garciaMarquez, novela, penguin, 1967);
        var rayuela = await ObtenerOCrearLibroAsync(context, "9788437604572", "Rayuela", cortazar, novela, planeta, 1963);
        var casaEspiritus = await ObtenerOCrearLibroAsync(context, "9780553383805", "La casa de los espiritus", allende, novela, penguin, 1982);
        var tunel = await ObtenerOCrearLibroAsync(context, "9789875666481", "El tunel", sabato, novela, planeta, 1948);
        var historiaLectura = await ObtenerOCrearLibroAsync(context, "9780306406157", "Historia de la lectura", cortazar, historia, sigloXxi, 1996);

        await context.SaveChangesAsync();

        return new Dictionary<string, Libro>
        {
            ["Cien anios de soledad"] = cienAnios,
            ["Rayuela"] = rayuela,
            ["La casa de los espiritus"] = casaEspiritus,
            ["El tunel"] = tunel,
            ["Historia de la lectura"] = historiaLectura
        };
    }

    private static async Task CrearStockInicialAsync(BibliotecaContext context, Dictionary<string, Libro> libros)
    {
        var stockInicial = new Dictionary<string, int>
        {
            ["Cien anios de soledad"] = 5,
            ["Rayuela"] = 4,
            ["La casa de los espiritus"] = 3,
            ["El tunel"] = 2,
            ["Historia de la lectura"] = 6
        };

        foreach (var (titulo, cantidad) in stockInicial)
        {
            var libro = libros[titulo];
            var existe = await context.MovimientosStock.AnyAsync(m =>
                m.IdLibro == libro.IdLibro && m.Motivo == "Carga inicial de datos");

            if (existe)
            {
                continue;
            }

            libro.StockTotal += cantidad;
            libro.StockDisponible += cantidad;
            context.MovimientosStock.Add(new MovimientoStock
            {
                IdLibro = libro.IdLibro,
                TipoMovimiento = TipoMovimientoStock.Devolucion,
                Cantidad = cantidad,
                Motivo = "Carga inicial de datos",
                Fecha = DateTime.Now
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task CrearPrestamosAsync(
        BibliotecaContext context,
        Dictionary<string, Empleado> empleados,
        Dictionary<string, Cliente> clientes,
        Dictionary<string, Libro> libros)
    {
        if (await context.Prestamos.AnyAsync())
        {
            return;
        }

        var hoy = DateTime.Today;

        await RegistrarPrestamoAsync(
            context,
            empleados["Juan Perez"].IdPersona,
            clientes["Ana Maria Perez"].IdPersona,
            new[] { libros["Cien anios de soledad"].IdLibro, libros["Rayuela"].IdLibro },
            hoy.AddDays(-3).AddHours(9).AddMinutes(30),
            hoy.AddDays(12));

        await RegistrarPrestamoAsync(
            context,
            empleados["Juan Perez"].IdPersona,
            clientes["Lucia Fernandez"].IdPersona,
            new[] { libros["La casa de los espiritus"].IdLibro },
            hoy.AddDays(-15).AddHours(14).AddMinutes(15),
            hoy.AddDays(-5));

        var prestamoDevuelto = await RegistrarPrestamoAsync(
            context,
            empleados["Carlos Gomez"].IdPersona,
            clientes["Martin Silva"].IdPersona,
            new[] { libros["El tunel"].IdLibro },
            hoy.AddDays(-20).AddHours(11),
            hoy.AddDays(-10));

        await RegistrarDevolucionAsync(
            context,
            prestamoDevuelto.ItemsPrestamo.First().IdItemPrestamo,
            hoy.AddDays(-8).AddHours(10).AddMinutes(45));
    }

    private static async Task<Prestamo> RegistrarPrestamoAsync(
        BibliotecaContext context,
        int idEmpleado,
        int idCliente,
        IEnumerable<int> librosIds,
        DateTime fecha,
        DateTime fechaDevolucion)
    {
        var ids = librosIds.ToList();
        var cantidades = ids.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
        var libros = await context.Libros.Where(l => cantidades.Keys.Contains(l.IdLibro)).ToListAsync();

        var prestamo = new Prestamo
        {
            IdEmpleadoRegistro = idEmpleado,
            IdCliente = idCliente,
            Fecha = fecha,
            FechaEstimadaDevolucion = fechaDevolucion,
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

        context.Prestamos.Add(prestamo);
        foreach (var libro in libros)
        {
            libro.StockDisponible -= cantidades[libro.IdLibro];
        }

        await context.SaveChangesAsync();

        foreach (var libro in libros)
        {
            context.MovimientosStock.Add(new MovimientoStock
            {
                IdLibro = libro.IdLibro,
                IdPrestamo = prestamo.IdPrestamo,
                IdEmpleado = idEmpleado,
                TipoMovimiento = TipoMovimientoStock.Prestado,
                Cantidad = cantidades[libro.IdLibro],
                Motivo = "Prestamo registrado",
                Fecha = fecha
            });
        }

        await context.SaveChangesAsync();
        return prestamo;
    }

    private static async Task RegistrarDevolucionAsync(BibliotecaContext context, int idItemPrestamo, DateTime fecha)
    {
        var item = await context.ItemsPrestamo
            .Include(i => i.Libro)
            .Include(i => i.Prestamo)
            .FirstAsync(i => i.IdItemPrestamo == idItemPrestamo);

        var diasRetraso = Math.Max(0, (fecha.Date - item.Prestamo.FechaEstimadaDevolucion.Date).Days);
        item.FechaDevolucion = fecha;
        item.DiasRetraso = diasRetraso;
        item.EstadoItem = diasRetraso > 0 ? EstadoItemPrestamo.Vencido : EstadoItemPrestamo.Devuelto;
        item.Libro.StockDisponible += 1;
        item.Prestamo.FechaDevolucion = fecha;
        item.Prestamo.Estado = "Finalizado";

        context.MovimientosStock.Add(new MovimientoStock
        {
            IdLibro = item.IdLibro,
            IdPrestamo = item.Prestamo.IdPrestamo,
            IdEmpleado = item.Prestamo.IdEmpleadoRegistro,
            TipoMovimiento = TipoMovimientoStock.Devolucion,
            Cantidad = 1,
            Motivo = "Devolucion de prestamo",
            Fecha = fecha
        });

        await context.SaveChangesAsync();
    }

    private static async Task<Empleado> ObtenerOCrearEmpleadoAsync(
        BibliotecaContext context, string dni, string nombre, string apellido, string telefono, string direccion)
    {
        var empleado = await context.Empleados.FirstOrDefaultAsync(e => e.DNI == dni);
        if (empleado is null)
        {
            empleado = new Empleado { DNI = dni, Activo = true };
            context.Empleados.Add(empleado);
        }

        ActualizarPersona(empleado, nombre, apellido, telefono, direccion);
        return empleado;
    }

    private static async Task<Cliente> ObtenerOCrearClienteAsync(
        BibliotecaContext context, string dni, string nombre, string apellido, string telefono, string email, string direccion)
    {
        var cliente = await context.Clientes.FirstOrDefaultAsync(c => c.DNI == dni);
        if (cliente is null)
        {
            cliente = new Cliente { DNI = dni };
            context.Clientes.Add(cliente);
        }

        ActualizarPersona(cliente, nombre, apellido, telefono, direccion);
        cliente.Email = email;
        return cliente;
    }

    private static void ActualizarPersona(Persona persona, string nombre, string apellido, string telefono, string direccion)
    {
        persona.Nombre = nombre;
        persona.Apellido = apellido;
        persona.Telefono = telefono;
        persona.Direccion = direccion;
    }

    private static async Task<Usuario> ObtenerOCrearUsuarioAsync(
        BibliotecaContext context, int idEmpleado, string cuenta, string password, string rol)
    {
        var cuentaNormalizada = cuenta.Trim().ToLowerInvariant();
        var usuarioPorEmpleado = await context.Usuarios.FirstOrDefaultAsync(u => u.IdEmpleado == idEmpleado);
        var usuarioPorCuenta = await context.Usuarios.FirstOrDefaultAsync(u => u.Email == cuentaNormalizada);
        var usuario = usuarioPorEmpleado ?? usuarioPorCuenta;

        if (usuario is null)
        {
            usuario = new Usuario { IdEmpleado = idEmpleado };
            context.Usuarios.Add(usuario);
        }
        else if (usuario.IdEmpleado != idEmpleado && usuarioPorEmpleado is null)
        {
            usuario.IdEmpleado = idEmpleado;
        }

        if (usuarioPorCuenta is null || usuarioPorCuenta.IdUsuarioSistema == usuario.IdUsuarioSistema)
        {
            usuario.Email = cuentaNormalizada;
        }

        usuario.Password = password;
        usuario.Rol = rol;
        usuario.Activo = true;
        return usuario;
    }

    private static async Task<Autor> ObtenerOCrearAutorAsync(BibliotecaContext context, string nombre, string apellido, string nacionalidad)
    {
        var autor = await context.Autores.FirstOrDefaultAsync(a => a.Nombre == nombre && a.Apellido == apellido);
        if (autor is not null) return autor;
        autor = new Autor { Nombre = nombre, Apellido = apellido, Nacionalidad = nacionalidad };
        context.Autores.Add(autor);
        return autor;
    }

    private static async Task<Categoria> ObtenerOCrearCategoriaAsync(BibliotecaContext context, string nombre, string descripcion)
    {
        var categoria = await context.Categorias.FirstOrDefaultAsync(c => c.Nombre == nombre);
        if (categoria is not null) return categoria;
        categoria = new Categoria { Nombre = nombre, Descripcion = descripcion };
        context.Categorias.Add(categoria);
        return categoria;
    }

    private static async Task<Editorial> ObtenerOCrearEditorialAsync(BibliotecaContext context, string nombre, string descripcion)
    {
        var editorial = await context.Editoriales.FirstOrDefaultAsync(e => e.Nombre == nombre);
        if (editorial is not null) return editorial;
        editorial = new Editorial { Nombre = nombre, Descripcion = descripcion };
        context.Editoriales.Add(editorial);
        return editorial;
    }

    private static async Task<Libro> ObtenerOCrearLibroAsync(
        BibliotecaContext context, string isbn, string titulo, Autor autor, Categoria categoria,
        Editorial editorial, int anioPublicacion)
    {
        var libro = await context.Libros.FirstOrDefaultAsync(l => l.ISBN == isbn);
        if (libro is not null) return libro;

        libro = new Libro
        {
            ISBN = isbn,
            Titulo = titulo,
            Autor = autor,
            Categoria = categoria,
            Editorial = editorial,
            AnioPublicacion = anioPublicacion,
            Activo = true
        };
        context.Libros.Add(libro);
        return libro;
    }
}

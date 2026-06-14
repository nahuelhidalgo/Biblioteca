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

        var empleados = await CrearEmpleadosYUsuariosAsync(context);
        var libros = await CrearCatalogoAsync(context);

        await CrearStockInicialAsync(context, libros);
        await CrearPrestamosAsync(context, empleados, libros);
    }

    private static async Task<Dictionary<string, Empleado>> CrearEmpleadosYUsuariosAsync(BibliotecaContext context)
    {
        var empleado1 = await ObtenerOCrearEmpleadoAsync(
            context,
            dni: "30123456",
            nombre: "Juan",
            apellido: "Perez",
            telefono: "1123456789",
            direccion: "Sucursal Central");

        var empleado2 = await ObtenerOCrearEmpleadoAsync(
            context,
            dni: "30234567",
            nombre: "Ana Maria",
            apellido: "Perez",
            telefono: "1134567890",
            direccion: "Sucursal Central");

        var gerente = await ObtenerOCrearEmpleadoAsync(
            context,
            dni: "30345678",
            nombre: "Carlos",
            apellido: "Gomez",
            telefono: "1145678901",
            direccion: "Administracion");

        var operadorSistema = await ObtenerOCrearEmpleadoAsync(
            context,
            dni: "30456789",
            nombre: "Lucia",
            apellido: "Fernandez",
            telefono: "1156789012",
            direccion: "Sucursal Central");

        var administradorSistema = await ObtenerOCrearEmpleadoAsync(
            context,
            dni: "30567890",
            nombre: "Martin",
            apellido: "Silva",
            telefono: "1167890123",
            direccion: "Administracion");

        await context.SaveChangesAsync();

        await ObtenerOCrearUsuarioPorEmpleadoAsync(
            context,
            cuenta: "operador1@ort.edu.ar",
            password: "Operador1",
            rol: "Operador",
            legajoEmpleado: empleado1.Legajo);

        await ObtenerOCrearUsuarioPorEmpleadoAsync(
            context,
            cuenta: "operador2@ort.edu.ar",
            password: "Operador2",
            rol: "Operador",
            legajoEmpleado: empleado2.Legajo);

        await ObtenerOCrearUsuarioPorEmpleadoAsync(
            context,
            cuenta: "operador3@ort.edu.ar",
            password: "Operador3",
            rol: "Operador",
            legajoEmpleado: operadorSistema.Legajo);

        await ObtenerOCrearUsuarioPorEmpleadoAsync(
            context,
            cuenta: "administrador1@ort.edu.ar",
            password: "Administrador1",
            rol: "Administrador",
            legajoEmpleado: gerente.Legajo);

        await ObtenerOCrearUsuarioPorEmpleadoAsync(
            context,
            cuenta: "administrador2@ort.edu.ar",
            password: "Administrador2",
            rol: "Administrador",
            legajoEmpleado: administradorSistema.Legajo);

        await context.SaveChangesAsync();

        return new Dictionary<string, Empleado>
        {
            ["Empleado 1"] = empleado1,
            ["Empleado 2"] = empleado2,
            ["Gerente"] = gerente,
            ["Operador"] = operadorSistema,
            ["Administrador"] = administradorSistema
        };
    }

    private static async Task<Dictionary<string, Libro>> CrearCatalogoAsync(BibliotecaContext context)
    {
        var garciaMarquez = await ObtenerOCrearAutorAsync(context, "Gabriel", "Garcia Marquez", "Colombia");
        var cortazar = await ObtenerOCrearAutorAsync(context, "Julio", "Cortazar", "Argentina");
        var allende = await ObtenerOCrearAutorAsync(context, "Isabel", "Allende", "Chile");
        var sabato = await ObtenerOCrearAutorAsync(context, "Ernesto", "Sabato", "Argentina");

        var novela = await ObtenerOCrearCategoriaAsync(context, "Novela", "Ficcion narrativa.");
        var ensayo = await ObtenerOCrearCategoriaAsync(context, "Ensayo", "Textos de analisis y reflexion.");
        var historia = await ObtenerOCrearCategoriaAsync(context, "Historia", "Libros de historia y divulgacion historica.");

        var planeta = await ObtenerOCrearEditorialAsync(context, "Planeta", "Grupo editorial Planeta.");
        var penguin = await ObtenerOCrearEditorialAsync(context, "Penguin Random House", "Editorial internacional.");
        var sigloXxi = await ObtenerOCrearEditorialAsync(context, "Siglo XXI", "Publicaciones academicas.");

        await context.SaveChangesAsync();

        var cienAnios = await ObtenerOCrearLibroAsync(
            context,
            isbn: "9780307474728",
            titulo: "Cien anios de soledad",
            autor: garciaMarquez,
            categoria: novela,
            editorial: penguin,
            anioPublicacion: 1967);

        var rayuela = await ObtenerOCrearLibroAsync(
            context,
            isbn: "9788437604572",
            titulo: "Rayuela",
            autor: cortazar,
            categoria: novela,
            editorial: planeta,
            anioPublicacion: 1963);

        var casaEspiritus = await ObtenerOCrearLibroAsync(
            context,
            isbn: "9780553383805",
            titulo: "La casa de los espiritus",
            autor: allende,
            categoria: novela,
            editorial: penguin,
            anioPublicacion: 1982);

        var tunel = await ObtenerOCrearLibroAsync(
            context,
            isbn: "9789875666481",
            titulo: "El tunel",
            autor: sabato,
            categoria: novela,
            editorial: planeta,
            anioPublicacion: 1948);

        var historiaLectura = await ObtenerOCrearLibroAsync(
            context,
            isbn: "9780306406157",
            titulo: "Historia de la lectura",
            autor: cortazar,
            categoria: historia,
            editorial: sigloXxi,
            anioPublicacion: 1996);

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

    private static async Task CrearStockInicialAsync(
        BibliotecaContext context,
        Dictionary<string, Libro> libros)
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

            var tieneMovimientoInicial = await context.MovimientosStock.AnyAsync(m =>
                m.IdLibro == libro.IdLibro && m.Motivo == "Carga inicial de datos");

            if (!tieneMovimientoInicial)
            {
                libro.StockTotal += cantidad;
                libro.StockDisponible += cantidad;

            context.MovimientosStock.Add(new MovimientoStock
            {
                IdLibro = libro.IdLibro,
                TipoMovimiento = TipoMovimientoStock.AltaStock,
                Cantidad = cantidad,
                    Motivo = "Carga inicial de datos",
                    Fecha = DateTime.Now
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task CrearPrestamosAsync(
        BibliotecaContext context,
        Dictionary<string, Empleado> empleados,
        Dictionary<string, Libro> libros)
    {
        if (await context.Prestamos.AnyAsync())
        {
            return;
        }

        await RegistrarPrestamoDeSemillaAsync(
            context,
            empleados["Empleado 1"].Legajo,
            new[] { libros["Cien anios de soledad"].IdLibro, libros["Rayuela"].IdLibro },
            DateTime.Today.AddDays(-3),
            DateTime.Today.AddDays(12));

        await RegistrarPrestamoDeSemillaAsync(
            context,
            empleados["Empleado 2"].Legajo,
            new[] { libros["La casa de los espiritus"].IdLibro },
            DateTime.Today.AddDays(-15),
            DateTime.Today.AddDays(-5));

        var prestamoDevuelto = await RegistrarPrestamoDeSemillaAsync(
            context,
            empleados["Gerente"].Legajo,
            new[] { libros["El tunel"].IdLibro },
            DateTime.Today.AddDays(-20),
            DateTime.Today.AddDays(-10));

        var itemDevuelto = prestamoDevuelto.ItemsPrestamo.First();

        await RegistrarDevolucionDeSemillaAsync(context, itemDevuelto.IdItemPrestamo, DateTime.Today.AddDays(-8));
    }

    private static async Task<Prestamo> RegistrarPrestamoDeSemillaAsync(
        BibliotecaContext context,
        int legajoEmpleado,
        IEnumerable<int> librosIds,
        DateTime fechaPrestamo,
        DateTime fechaEstimadaDevolucion)
    {
        var ids = librosIds.ToList();
        var cantidadesPorLibro = ids
            .GroupBy(id => id)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Count());

        var libros = await context.Libros
            .Where(libro => cantidadesPorLibro.Keys.Contains(libro.IdLibro))
            .ToListAsync();

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

        context.Prestamos.Add(prestamo);

        foreach (var libro in libros)
        {
            var cantidad = cantidadesPorLibro[libro.IdLibro];
            libro.StockDisponible -= cantidad;

            context.MovimientosStock.Add(new MovimientoStock
            {
                IdLibro = libro.IdLibro,
                LegajoEmpleado = legajoEmpleado,
                TipoMovimiento = TipoMovimientoStock.Prestado,
                Cantidad = cantidad,
                Motivo = "Prestamo registrado",
                Fecha = DateTime.Now
            });
        }

        await context.SaveChangesAsync();

        return prestamo;
    }

    private static async Task RegistrarDevolucionDeSemillaAsync(
        BibliotecaContext context,
        int idItemPrestamo,
        DateTime fechaDevolucion)
    {
        var item = await context.ItemsPrestamo
            .Include(i => i.Libro)
            .Include(i => i.Prestamo)
            .FirstAsync(i => i.IdItemPrestamo == idItemPrestamo);

        var diasRetraso = Math.Max(0, (fechaDevolucion.Date - item.Prestamo.FechaEstimadaDevolucion.Date).Days);

        item.FechaDevolucion = fechaDevolucion;
        item.DiasRetraso = diasRetraso;
        item.EstadoItem = diasRetraso > 0 ? EstadoItemPrestamo.Vencido : EstadoItemPrestamo.Devuelto;
        item.Libro.StockDisponible += 1;
        item.Prestamo.FechaDevolucion = fechaDevolucion;
        item.Prestamo.Estado = "Finalizado";

        context.MovimientosStock.Add(new MovimientoStock
        {
            IdLibro = item.IdLibro,
            LegajoEmpleado = item.Prestamo.LegajoEmpleado,
            TipoMovimiento = TipoMovimientoStock.AltaStock,
            Cantidad = 1,
            Motivo = "Devolucion de prestamo",
            Fecha = DateTime.Now
        });

        await context.SaveChangesAsync();
    }

    private static async Task<Empleado> ObtenerOCrearEmpleadoAsync(
        BibliotecaContext context,
        string dni,
        string nombre,
        string apellido,
        string telefono,
        string direccion)
    {
        var empleado = await context.Empleados.FirstOrDefaultAsync(e => e.DNI == dni);

        if (empleado is not null)
        {
            empleado.Nombre = nombre;
            empleado.Apellido = apellido;
            empleado.Telefono = telefono;
            empleado.Direccion = direccion;

            return empleado;
        }

        empleado = new Empleado
        {
            DNI = dni,
            Nombre = nombre,
            Apellido = apellido,
            Telefono = telefono,
            Direccion = direccion
        };

        context.Empleados.Add(empleado);

        return empleado;
    }

    private static async Task<Usuario> ObtenerOCrearUsuarioPorEmpleadoAsync(
        BibliotecaContext context,
        string cuenta,
        string password,
        string rol,
        int legajoEmpleado)
    {
        var usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.LegajoEmpleado == legajoEmpleado);

        if (usuario is not null)
        {
            usuario.Email = cuenta;
            usuario.Password = password;
            usuario.Rol = rol;
            usuario.Activo = true;

            return usuario;
        }

        usuario = new Usuario
        {
            Email = cuenta,
            Password = password,
            Rol = rol,
            LegajoEmpleado = legajoEmpleado,
            Activo = true
        };

        context.Usuarios.Add(usuario);

        return usuario;
    }

    private static async Task<Autor> ObtenerOCrearAutorAsync(
        BibliotecaContext context,
        string nombre,
        string apellido,
        string nacionalidad)
    {
        var autor = await context.Autores.FirstOrDefaultAsync(a => a.Nombre == nombre && a.Apellido == apellido);

        if (autor is not null)
        {
            return autor;
        }

        autor = new Autor
        {
            Nombre = nombre,
            Apellido = apellido,
            Nacionalidad = nacionalidad
        };

        context.Autores.Add(autor);

        return autor;
    }

    private static async Task<Categoria> ObtenerOCrearCategoriaAsync(
        BibliotecaContext context,
        string nombre,
        string descripcion)
    {
        var categoria = await context.Categorias.FirstOrDefaultAsync(c => c.Nombre == nombre);

        if (categoria is not null)
        {
            return categoria;
        }

        categoria = new Categoria
        {
            Nombre = nombre,
            Descripcion = descripcion
        };

        context.Categorias.Add(categoria);

        return categoria;
    }

    private static async Task<Editorial> ObtenerOCrearEditorialAsync(
        BibliotecaContext context,
        string nombre,
        string descripcion)
    {
        var editorial = await context.Editoriales.FirstOrDefaultAsync(e => e.Nombre == nombre);

        if (editorial is not null)
        {
            return editorial;
        }

        editorial = new Editorial
        {
            Nombre = nombre,
            Descripcion = descripcion
        };

        context.Editoriales.Add(editorial);

        return editorial;
    }

    private static async Task<Libro> ObtenerOCrearLibroAsync(
        BibliotecaContext context,
        string isbn,
        string titulo,
        Autor autor,
        Categoria categoria,
        Editorial editorial,
        int anioPublicacion)
    {
        var libro = await context.Libros.FirstOrDefaultAsync(l => l.ISBN == isbn);

        if (libro is not null)
        {
            return libro;
        }

        libro = new Libro
        {
            ISBN = isbn,
            Titulo = titulo,
            Autor = autor,
            Categoria = categoria,
            Editorial = editorial,
            AnioPublicacion = anioPublicacion,
            StockTotal = 0,
            StockDisponible = 0,
            Activo = true
        };

        context.Libros.Add(libro);

        return libro;
    }
}

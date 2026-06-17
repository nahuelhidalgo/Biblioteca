using Biblioteca.Modelos;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Datos;

public class BibliotecaContext : DbContext
{
    public BibliotecaContext(DbContextOptions<BibliotecaContext> options)
        : base(options)
    {
    }

    public DbSet<Autor> Autores => Set<Autor>();

    public DbSet<Categoria> Categorias => Set<Categoria>();

    public DbSet<Editorial> Editoriales => Set<Editorial>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Empleado> Empleados => Set<Empleado>();

    public DbSet<Libro> Libros => Set<Libro>();

    public DbSet<Prestamo> Prestamos => Set<Prestamo>();

    public DbSet<ItemPrestamo> ItemsPrestamo => Set<ItemPrestamo>();

    public DbSet<MovimientoStock> MovimientosStock => Set<MovimientoStock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Autor>(entity =>
        {
            entity.HasKey(e => e.IdAutor);
            entity.HasIndex(e => new { e.Nombre, e.Apellido }).IsUnique();
        });

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(e => e.IdCategoria);
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        modelBuilder.Entity<Editorial>(entity =>
        {
            entity.HasKey(e => e.IdEditorial);
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuarioSistema);
            entity.HasIndex(e => e.Email).IsUnique();

            entity.HasOne(e => e.Empleado)
                .WithOne(e => e.Usuario)
                .HasForeignKey<Usuario>(e => e.LegajoEmpleado)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.HasKey(e => e.Legajo);
            entity.HasIndex(e => e.DNI).IsUnique();
        });

        modelBuilder.Entity<Libro>(entity =>
        {
            entity.HasKey(e => e.IdLibro);
            entity.HasIndex(e => e.ISBN).IsUnique();

            entity.HasOne(e => e.Autor)
                .WithMany(e => e.Libros)
                .HasForeignKey(e => e.IdAutor)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Categoria)
                .WithMany(e => e.Libros)
                .HasForeignKey(e => e.IdCategoria)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Editorial)
                .WithMany(e => e.Libros)
                .HasForeignKey(e => e.IdEditorial)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Libros_AnioPublicacion", "[AnioPublicacion] >= 1000");
                t.HasCheckConstraint("CK_Libros_StockTotal", "[StockTotal] >= 0");
                t.HasCheckConstraint("CK_Libros_StockDisponible", "[StockDisponible] >= 0");
                t.HasCheckConstraint("CK_Libros_StockDisponible_Total", "[StockDisponible] <= [StockTotal]");
            });
        });

        modelBuilder.Entity<Prestamo>(entity =>
        {
            entity.HasKey(e => e.IdPrestamo);

            entity.HasOne(e => e.Empleado)
                .WithMany(e => e.Prestamos)
                .HasForeignKey(e => e.LegajoEmpleado)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ItemPrestamo>(entity =>
        {
            entity.HasKey(e => e.IdItemPrestamo);

            entity.Property(e => e.EstadoItem)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.HasOne(e => e.Prestamo)
                .WithMany(e => e.ItemsPrestamo)
                .HasForeignKey(e => e.IdPrestamo)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Libro)
                .WithMany(e => e.ItemsPrestamo)
                .HasForeignKey(e => e.IdLibro)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MovimientoStock>(entity =>
        {
            entity.HasKey(e => e.IdMovimientoStock);

            entity.HasIndex(e => e.IdPrestamo);

            entity.Property(e => e.TipoMovimiento)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.HasOne(e => e.Libro)
                .WithMany(e => e.MovimientosStock)
                .HasForeignKey(e => e.IdLibro)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Empleado)
                .WithMany(e => e.MovimientosStock)
                .HasForeignKey(e => e.LegajoEmpleado)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_MovimientosStock_Cantidad", "[Cantidad] > 0");
            });
        });
    }
}

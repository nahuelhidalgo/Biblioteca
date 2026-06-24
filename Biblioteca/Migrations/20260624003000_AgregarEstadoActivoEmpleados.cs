using Biblioteca.Datos;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biblioteca.Migrations
{
    [Migration("20260624003000_AgregarEstadoActivoEmpleados")]
    [DbContext(typeof(BibliotecaContext))]
    public partial class AgregarEstadoActivoEmpleados : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE Personas SET Activo = 1 WHERE TipoPersona = 'Empleado' AND Activo IS NULL;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE Personas SET Activo = NULL WHERE TipoPersona = 'Empleado';");
        }
    }
}

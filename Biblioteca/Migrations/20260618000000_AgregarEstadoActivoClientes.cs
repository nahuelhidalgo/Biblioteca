using Biblioteca.Datos;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biblioteca.Migrations
{
    [Migration("20260618000000_AgregarEstadoActivoClientes")]
    [DbContext(typeof(BibliotecaContext))]
    public partial class AgregarEstadoActivoClientes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Personas",
                type: "bit",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE Personas SET Activo = 1 WHERE TipoPersona = 'Cliente';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Personas");
        }
    }
}

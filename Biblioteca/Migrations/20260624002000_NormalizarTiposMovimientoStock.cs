using Biblioteca.Datos;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biblioteca.Migrations
{
    [Migration("20260624002000_NormalizarTiposMovimientoStock")]
    [DbContext(typeof(BibliotecaContext))]
    public partial class NormalizarTiposMovimientoStock : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE MovimientosStock
                SET TipoMovimiento = 'Devolucion'
                WHERE TipoMovimiento = 'AltaStock';

                UPDATE MovimientosStock
                SET TipoMovimiento = 'Prestado'
                WHERE TipoMovimiento = 'BajaStock';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE MovimientosStock
                SET TipoMovimiento = 'AltaStock'
                WHERE TipoMovimiento = 'Devolucion';
                """);
        }
    }
}

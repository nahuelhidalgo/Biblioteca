using Biblioteca.Datos;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biblioteca.Migrations
{
    [Migration("20260623233000_AgregarRelacionPrestamoMovimientoStock")]
    [DbContext(typeof(BibliotecaContext))]
    public partial class AgregarRelacionPrestamoMovimientoStock : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE MovimientosStock
                SET IdPrestamo = NULL
                WHERE IdPrestamo IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM Prestamos
                      WHERE Prestamos.IdPrestamo = MovimientosStock.IdPrestamo
                  );
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosStock_Prestamos_IdPrestamo",
                table: "MovimientosStock",
                column: "IdPrestamo",
                principalTable: "Prestamos",
                principalColumn: "IdPrestamo",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosStock_Prestamos_IdPrestamo",
                table: "MovimientosStock");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biblioteca.Migrations
{
    [Migration("20260613190000_AgregarResponsableMovimientoStock")]
    public partial class AgregarResponsableMovimientoStock : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LegajoEmpleado",
                table: "MovimientosStock",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_LegajoEmpleado",
                table: "MovimientosStock",
                column: "LegajoEmpleado");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosStock_Empleados_LegajoEmpleado",
                table: "MovimientosStock",
                column: "LegajoEmpleado",
                principalTable: "Empleados",
                principalColumn: "Legajo",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosStock_Empleados_LegajoEmpleado",
                table: "MovimientosStock");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosStock_LegajoEmpleado",
                table: "MovimientosStock");

            migrationBuilder.DropColumn(
                name: "LegajoEmpleado",
                table: "MovimientosStock");
        }
    }
}

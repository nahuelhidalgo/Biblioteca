using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biblioteca.Migrations
{
    /// <inheritdoc />
    public partial class SepararPersonasEmpleadosYClientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosStock_Empleados_LegajoEmpleado",
                table: "MovimientosStock");

            migrationBuilder.DropForeignKey(
                name: "FK_Prestamos_Empleados_LegajoEmpleado",
                table: "Prestamos");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Empleados_LegajoEmpleado",
                table: "Usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Empleados",
                table: "Empleados");

            migrationBuilder.RenameTable(
                name: "Empleados",
                newName: "Personas");

            migrationBuilder.RenameColumn(
                name: "LegajoEmpleado",
                table: "Usuarios",
                newName: "IdEmpleado");

            migrationBuilder.RenameIndex(
                name: "IX_Usuarios_LegajoEmpleado",
                table: "Usuarios",
                newName: "IX_Usuarios_IdEmpleado");

            migrationBuilder.RenameColumn(
                name: "LegajoEmpleado",
                table: "Prestamos",
                newName: "IdEmpleadoRegistro");

            migrationBuilder.RenameIndex(
                name: "IX_Prestamos_LegajoEmpleado",
                table: "Prestamos",
                newName: "IX_Prestamos_IdEmpleadoRegistro");

            migrationBuilder.RenameColumn(
                name: "LegajoEmpleado",
                table: "MovimientosStock",
                newName: "IdEmpleado");

            migrationBuilder.RenameIndex(
                name: "IX_MovimientosStock_LegajoEmpleado",
                table: "MovimientosStock",
                newName: "IX_MovimientosStock_IdEmpleado");

            migrationBuilder.RenameColumn(
                name: "Legajo",
                table: "Personas",
                newName: "IdPersona");

            migrationBuilder.RenameIndex(
                name: "IX_Empleados_DNI",
                table: "Personas",
                newName: "IX_Personas_DNI");

            migrationBuilder.AddColumn<int>(
                name: "IdCliente",
                table: "Prestamos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoPersona",
                table: "Personas",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "Empleado");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Personas",
                table: "Personas",
                column: "IdPersona");

            migrationBuilder.Sql(
                """
                DECLARE @JuanId int = (SELECT TOP 1 IdPersona FROM Personas WHERE DNI = '30123456');
                DECLARE @AnaId int = (SELECT TOP 1 IdPersona FROM Personas WHERE DNI = '30234567');
                DECLARE @CarlosId int = (SELECT TOP 1 IdPersona FROM Personas WHERE DNI = '30345678');
                DECLARE @LuciaId int = (SELECT TOP 1 IdPersona FROM Personas WHERE DNI = '30456789');
                DECLARE @MartinId int = (SELECT TOP 1 IdPersona FROM Personas WHERE DNI = '30567890');

                DELETE usuario
                FROM Usuarios AS usuario
                INNER JOIN Personas AS persona ON persona.IdPersona = usuario.IdEmpleado
                WHERE persona.DNI IN ('30234567', '30456789', '30567890');

                UPDATE movimiento
                SET IdEmpleado = CASE
                    WHEN responsable.DNI IN ('30345678', '30567890') THEN @CarlosId
                    ELSE @JuanId
                END
                FROM MovimientosStock AS movimiento
                INNER JOIN Personas AS responsable ON responsable.IdPersona = movimiento.IdEmpleado;

                UPDATE prestamo
                SET IdCliente = CASE responsable.DNI
                        WHEN '30123456' THEN @AnaId
                        WHEN '30234567' THEN @LuciaId
                        WHEN '30345678' THEN @MartinId
                        WHEN '30456789' THEN @LuciaId
                        WHEN '30567890' THEN @MartinId
                        ELSE @AnaId
                    END,
                    IdEmpleadoRegistro = CASE
                        WHEN responsable.DNI IN ('30345678', '30567890') THEN @CarlosId
                        ELSE @JuanId
                    END
                FROM Prestamos AS prestamo
                INNER JOIN Personas AS responsable ON responsable.IdPersona = prestamo.IdEmpleadoRegistro;

                UPDATE Personas
                SET TipoPersona = CASE
                    WHEN DNI IN ('30234567', '30456789', '30567890') THEN 'Cliente'
                    ELSE 'Empleado'
                END;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "IdCliente",
                table: "Prestamos",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prestamos_IdCliente",
                table: "Prestamos",
                column: "IdCliente");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosStock_Personas_IdEmpleado",
                table: "MovimientosStock",
                column: "IdEmpleado",
                principalTable: "Personas",
                principalColumn: "IdPersona",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Prestamos_Personas_IdCliente",
                table: "Prestamos",
                column: "IdCliente",
                principalTable: "Personas",
                principalColumn: "IdPersona",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Prestamos_Personas_IdEmpleadoRegistro",
                table: "Prestamos",
                column: "IdEmpleadoRegistro",
                principalTable: "Personas",
                principalColumn: "IdPersona",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Personas_IdEmpleado",
                table: "Usuarios",
                column: "IdEmpleado",
                principalTable: "Personas",
                principalColumn: "IdPersona",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosStock_Personas_IdEmpleado",
                table: "MovimientosStock");

            migrationBuilder.DropForeignKey(
                name: "FK_Prestamos_Personas_IdCliente",
                table: "Prestamos");

            migrationBuilder.DropForeignKey(
                name: "FK_Prestamos_Personas_IdEmpleadoRegistro",
                table: "Prestamos");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Personas_IdEmpleado",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Prestamos_IdCliente",
                table: "Prestamos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Personas",
                table: "Personas");

            migrationBuilder.DropColumn(
                name: "IdCliente",
                table: "Prestamos");

            migrationBuilder.DropColumn(
                name: "TipoPersona",
                table: "Personas");

            migrationBuilder.RenameTable(
                name: "Personas",
                newName: "Empleados");

            migrationBuilder.RenameColumn(
                name: "IdEmpleado",
                table: "Usuarios",
                newName: "LegajoEmpleado");

            migrationBuilder.RenameIndex(
                name: "IX_Usuarios_IdEmpleado",
                table: "Usuarios",
                newName: "IX_Usuarios_LegajoEmpleado");

            migrationBuilder.RenameColumn(
                name: "IdEmpleadoRegistro",
                table: "Prestamos",
                newName: "LegajoEmpleado");

            migrationBuilder.RenameIndex(
                name: "IX_Prestamos_IdEmpleadoRegistro",
                table: "Prestamos",
                newName: "IX_Prestamos_LegajoEmpleado");

            migrationBuilder.RenameColumn(
                name: "IdEmpleado",
                table: "MovimientosStock",
                newName: "LegajoEmpleado");

            migrationBuilder.RenameIndex(
                name: "IX_MovimientosStock_IdEmpleado",
                table: "MovimientosStock",
                newName: "IX_MovimientosStock_LegajoEmpleado");

            migrationBuilder.RenameColumn(
                name: "IdPersona",
                table: "Empleados",
                newName: "Legajo");

            migrationBuilder.RenameIndex(
                name: "IX_Personas_DNI",
                table: "Empleados",
                newName: "IX_Empleados_DNI");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Empleados",
                table: "Empleados",
                column: "Legajo");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosStock_Empleados_LegajoEmpleado",
                table: "MovimientosStock",
                column: "LegajoEmpleado",
                principalTable: "Empleados",
                principalColumn: "Legajo",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Prestamos_Empleados_LegajoEmpleado",
                table: "Prestamos",
                column: "LegajoEmpleado",
                principalTable: "Empleados",
                principalColumn: "Legajo",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Empleados_LegajoEmpleado",
                table: "Usuarios",
                column: "LegajoEmpleado",
                principalTable: "Empleados",
                principalColumn: "Legajo",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

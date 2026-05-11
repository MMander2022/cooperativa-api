using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CooperativaApp.Migrations
{
    /// <inheritdoc />
    public partial class FixPrecisionMoney : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DetallePago_Cuota_IdCuota",
                table: "DetallePago");

            migrationBuilder.DropForeignKey(
                name: "FK_DetallePago_Moras_IdMora",
                table: "DetallePago");

            migrationBuilder.DropForeignKey(
                name: "FK_DetallePago_Moras_MoraIdMora",
                table: "DetallePago");

            migrationBuilder.DropForeignKey(
                name: "FK_DetallePago_Pago_IdPago",
                table: "DetallePago");

            migrationBuilder.DropForeignKey(
                name: "FK_DetallePago_Pago_PagoIdPago",
                table: "DetallePago");

            migrationBuilder.DropIndex(
                name: "IX_DetallePago_MoraIdMora",
                table: "DetallePago");

            migrationBuilder.DropIndex(
                name: "IX_DetallePago_PagoIdPago",
                table: "DetallePago");

            migrationBuilder.DropColumn(
                name: "MoraIdMora",
                table: "DetallePago");

            migrationBuilder.DropColumn(
                name: "PagoIdPago",
                table: "DetallePago");

            migrationBuilder.AlterColumn<decimal>(
                name: "TasaInteres",
                table: "Credito",
                type: "decimal(8,4)",
                precision: 8,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddForeignKey(
                name: "FK_DetallePago_Cuota_IdCuota",
                table: "DetallePago",
                column: "IdCuota",
                principalTable: "Cuota",
                principalColumn: "IdCuota");

            migrationBuilder.AddForeignKey(
                name: "FK_DetallePago_Moras_IdMora",
                table: "DetallePago",
                column: "IdMora",
                principalTable: "Moras",
                principalColumn: "IdMora");

            migrationBuilder.AddForeignKey(
                name: "FK_DetallePago_Pago_IdPago",
                table: "DetallePago",
                column: "IdPago",
                principalTable: "Pago",
                principalColumn: "IdPago",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DetallePago_Cuota_IdCuota",
                table: "DetallePago");

            migrationBuilder.DropForeignKey(
                name: "FK_DetallePago_Moras_IdMora",
                table: "DetallePago");

            migrationBuilder.DropForeignKey(
                name: "FK_DetallePago_Pago_IdPago",
                table: "DetallePago");

            migrationBuilder.AddColumn<int>(
                name: "MoraIdMora",
                table: "DetallePago",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PagoIdPago",
                table: "DetallePago",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TasaInteres",
                table: "Credito",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,4)",
                oldPrecision: 8,
                oldScale: 4);

            migrationBuilder.CreateIndex(
                name: "IX_DetallePago_MoraIdMora",
                table: "DetallePago",
                column: "MoraIdMora");

            migrationBuilder.CreateIndex(
                name: "IX_DetallePago_PagoIdPago",
                table: "DetallePago",
                column: "PagoIdPago");

            migrationBuilder.AddForeignKey(
                name: "FK_DetallePago_Cuota_IdCuota",
                table: "DetallePago",
                column: "IdCuota",
                principalTable: "Cuota",
                principalColumn: "IdCuota",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DetallePago_Moras_IdMora",
                table: "DetallePago",
                column: "IdMora",
                principalTable: "Moras",
                principalColumn: "IdMora",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DetallePago_Moras_MoraIdMora",
                table: "DetallePago",
                column: "MoraIdMora",
                principalTable: "Moras",
                principalColumn: "IdMora");

            migrationBuilder.AddForeignKey(
                name: "FK_DetallePago_Pago_IdPago",
                table: "DetallePago",
                column: "IdPago",
                principalTable: "Pago",
                principalColumn: "IdPago",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DetallePago_Pago_PagoIdPago",
                table: "DetallePago",
                column: "PagoIdPago",
                principalTable: "Pago",
                principalColumn: "IdPago");
        }
    }
}

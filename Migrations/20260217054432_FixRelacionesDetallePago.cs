using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CooperativaApp.Migrations
{
    /// <inheritdoc />
    public partial class FixRelacionesDetallePago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AsientosContables",
                columns: table => new
                {
                    IdAsiento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TipoOperacion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReferenciaId = table.Column<int>(type: "int", nullable: false),
                    CuentaDebe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CuentaHaber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsientosContables", x => x.IdAsiento);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionMora",
                columns: table => new
                {
                    IdConfiguracion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoMora = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tasa = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoFijo = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DiasGracia = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    TipoAplicacion = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionMora", x => x.IdConfiguracion);
                });

            migrationBuilder.CreateTable(
                name: "Credito",
                columns: table => new
                {
                    IdCredito = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdSocio = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TasaInteres = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SaldoCapital = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PlazoMeses = table.Column<int>(type: "int", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaAprobacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoCalculo = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Credito", x => x.IdCredito);
                });

            migrationBuilder.CreateTable(
                name: "Pago",
                columns: table => new
                {
                    IdPago = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCredito = table.Column<int>(type: "int", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MontoPagado = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pago", x => x.IdPago);
                });

            migrationBuilder.CreateTable(
                name: "Socio",
                columns: table => new
                {
                    IdSocio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DNI = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nombres = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Apellidos = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Socio", x => x.IdSocio);
                });

            migrationBuilder.CreateTable(
                name: "Cuota",
                columns: table => new
                {
                    IdCuota = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCredito = table.Column<int>(type: "int", nullable: false),
                    NumeroCuota = table.Column<int>(type: "int", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Capital = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Interes = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SaldoCapital = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SaldoInteres = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoCuota = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Saldo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuota", x => x.IdCuota);
                    table.ForeignKey(
                        name: "FK_Cuota_Credito_IdCredito",
                        column: x => x.IdCredito,
                        principalTable: "Credito",
                        principalColumn: "IdCredito",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Moras",
                columns: table => new
                {
                    IdMora = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCuota = table.Column<int>(type: "int", nullable: false),
                    FechaGeneracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DiasMora = table.Column<int>(type: "int", nullable: true),
                    MontoMora = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoPagado = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SaldoMora = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CuotaIdCuota = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Moras", x => x.IdMora);
                    table.ForeignKey(
                        name: "FK_Moras_Cuota_CuotaIdCuota",
                        column: x => x.CuotaIdCuota,
                        principalTable: "Cuota",
                        principalColumn: "IdCuota");
                });

            migrationBuilder.CreateTable(
                name: "DetallePago",
                columns: table => new
                {
                    IdDetallePago = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdPago = table.Column<int>(type: "int", nullable: false),
                    MoraPagada = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InteresPagado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CapitalPagado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdCuota = table.Column<int>(type: "int", nullable: true),
                    IdMora = table.Column<int>(type: "int", nullable: true),
                    MoraIdMora = table.Column<int>(type: "int", nullable: true),
                    PagoIdPago = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallePago", x => x.IdDetallePago);
                    table.ForeignKey(
                        name: "FK_DetallePago_Cuota_IdCuota",
                        column: x => x.IdCuota,
                        principalTable: "Cuota",
                        principalColumn: "IdCuota",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallePago_Moras_IdMora",
                        column: x => x.IdMora,
                        principalTable: "Moras",
                        principalColumn: "IdMora",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallePago_Moras_MoraIdMora",
                        column: x => x.MoraIdMora,
                        principalTable: "Moras",
                        principalColumn: "IdMora");
                    table.ForeignKey(
                        name: "FK_DetallePago_Pago_IdPago",
                        column: x => x.IdPago,
                        principalTable: "Pago",
                        principalColumn: "IdPago",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallePago_Pago_PagoIdPago",
                        column: x => x.PagoIdPago,
                        principalTable: "Pago",
                        principalColumn: "IdPago");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cuota_IdCredito",
                table: "Cuota",
                column: "IdCredito");

            migrationBuilder.CreateIndex(
                name: "IX_DetallePago_IdCuota",
                table: "DetallePago",
                column: "IdCuota");

            migrationBuilder.CreateIndex(
                name: "IX_DetallePago_IdMora",
                table: "DetallePago",
                column: "IdMora");

            migrationBuilder.CreateIndex(
                name: "IX_DetallePago_IdPago",
                table: "DetallePago",
                column: "IdPago");

            migrationBuilder.CreateIndex(
                name: "IX_DetallePago_MoraIdMora",
                table: "DetallePago",
                column: "MoraIdMora");

            migrationBuilder.CreateIndex(
                name: "IX_DetallePago_PagoIdPago",
                table: "DetallePago",
                column: "PagoIdPago");

            migrationBuilder.CreateIndex(
                name: "IX_Moras_CuotaIdCuota",
                table: "Moras",
                column: "CuotaIdCuota");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsientosContables");

            migrationBuilder.DropTable(
                name: "ConfiguracionMora");

            migrationBuilder.DropTable(
                name: "DetallePago");

            migrationBuilder.DropTable(
                name: "Socio");

            migrationBuilder.DropTable(
                name: "Moras");

            migrationBuilder.DropTable(
                name: "Pago");

            migrationBuilder.DropTable(
                name: "Cuota");

            migrationBuilder.DropTable(
                name: "Credito");
        }
    }
}

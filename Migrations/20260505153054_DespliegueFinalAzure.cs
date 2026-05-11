using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CooperativaApp.Migrations
{
    /// <inheritdoc />
    public partial class DespliegueFinalAzure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallePago");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Pago",
                table: "Pago");

            migrationBuilder.DropColumn(
                name: "CuentaDebe",
                table: "AsientosContables");

            migrationBuilder.DropColumn(
                name: "CuentaHaber",
                table: "AsientosContables");

            migrationBuilder.DropColumn(
                name: "Monto",
                table: "AsientosContables");

            migrationBuilder.DropColumn(
                name: "ReferenciaId",
                table: "AsientosContables");

            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.RenameTable(
                name: "Pago",
                newName: "Pagos");

            migrationBuilder.RenameColumn(
                name: "TipoOperacion",
                table: "AsientosContables",
                newName: "Origen");

            migrationBuilder.RenameColumn(
                name: "Estado",
                table: "AsientosContables",
                newName: "Glosa");

            migrationBuilder.RenameColumn(
                name: "MontoPagado",
                table: "Pagos",
                newName: "MontoTotal");

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "Socio",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaRegistro",
                table: "Socio",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Direccion",
                table: "Socio",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DNI",
                table: "Socio",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ApellidoMaterno",
                table: "Socio",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApellidoPaterno",
                table: "Socio",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaModificacion",
                table: "Socio",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaNacimiento",
                table: "Socio",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdUsuarioModificacion",
                table: "Socio",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdUsuarioRegistro",
                table: "Socio",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "correo",
                table: "Socio",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EstadoCredito",
                table: "Credito",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaDesembolso",
                table: "Credito",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoDesembolsado",
                table: "Credito",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdReferencia",
                table: "AsientosContables",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdMedioPago",
                table: "Pagos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdSocio",
                table: "Pagos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IdUsuario",
                table: "Pagos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NroOperacion",
                table: "Pagos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Pagos",
                table: "Pagos",
                column: "IdPago");

            migrationBuilder.CreateTable(
                name: "AprobacionResponse",
                columns: table => new
                {
                    IdCreditoGenerado = table.Column<int>(type: "int", nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Exito = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "ConfigAportes",
                columns: table => new
                {
                    IdConfig = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ValorAccion = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdUsuarioRegistro = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigAportes", x => x.IdConfig);
                });

            migrationBuilder.CreateTable(
                name: "CuentasContables",
                columns: table => new
                {
                    CodigoCuenta = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NombreCuenta = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Nivel = table.Column<int>(type: "int", nullable: false),
                    Naturaleza = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    EsAnalitica = table.Column<bool>(type: "bit", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    CodigoCuentaPadre = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasContables", x => x.CodigoCuenta);
                });

            migrationBuilder.CreateTable(
                name: "DetalleAsiento",
                columns: table => new
                {
                    IdDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdAsiento = table.Column<int>(type: "int", nullable: false),
                    CuentaContable = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Debe = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Haber = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleAsiento", x => x.IdDetalle);
                    table.ForeignKey(
                        name: "FK_DetalleAsiento_AsientosContables_IdAsiento",
                        column: x => x.IdAsiento,
                        principalTable: "AsientosContables",
                        principalColumn: "IdAsiento",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GlobalSettings",
                columns: table => new
                {
                    SettingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SettingKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SettingValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalSettings", x => x.SettingId);
                });

            migrationBuilder.CreateTable(
                name: "Logs_Actividad",
                columns: table => new
                {
                    IdLog = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<int>(type: "int", nullable: true),
                    Accion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IP = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs_Actividad", x => x.IdLog);
                });

            migrationBuilder.CreateTable(
                name: "MediosPago",
                columns: table => new
                {
                    IdMedioPago = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediosPago", x => x.IdMedioPago);
                });

            migrationBuilder.CreateTable(
                name: "Modulos",
                columns: table => new
                {
                    IdModulo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ruta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Icono = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modulos", x => x.IdModulo);
                });

            migrationBuilder.CreateTable(
                name: "MotivosBaja",
                columns: table => new
                {
                    IdMotivo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    RequiereComentario = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotivosBaja", x => x.IdMotivo);
                });

            migrationBuilder.CreateTable(
                name: "OperacionResponses",
                columns: table => new
                {
                    IdOperacion = table.Column<int>(type: "int", nullable: true),
                    IdCredito = table.Column<int>(type: "int", nullable: true),
                    IdPago = table.Column<int>(type: "int", nullable: true),
                    IdMovimientoCaja = table.Column<int>(type: "int", nullable: true),
                    Mensaje = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Exito = table.Column<bool>(type: "bit", nullable: false),
                    SaldoPendienteDesembolso = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "PagosDetalle",
                columns: table => new
                {
                    IdPagoDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdPago = table.Column<int>(type: "int", nullable: false),
                    IdCuota = table.Column<int>(type: "int", nullable: false),
                    IdConcepto = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MoraIdMora = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosDetalle", x => x.IdPagoDetalle);
                    table.ForeignKey(
                        name: "FK_PagosDetalle_Cuota_IdCuota",
                        column: x => x.IdCuota,
                        principalTable: "Cuota",
                        principalColumn: "IdCuota",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PagosDetalle_Moras_MoraIdMora",
                        column: x => x.MoraIdMora,
                        principalTable: "Moras",
                        principalColumn: "IdMora");
                    table.ForeignKey(
                        name: "FK_PagosDetalle_Pagos_IdPago",
                        column: x => x.IdPago,
                        principalTable: "Pagos",
                        principalColumn: "IdPago",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Parentesco",
                columns: table => new
                {
                    IdParentesco = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parentesco", x => x.IdParentesco);
                });

            migrationBuilder.CreateTable(
                name: "Perfiles",
                columns: table => new
                {
                    IdPerfil = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perfiles", x => x.IdPerfil);
                });

            migrationBuilder.CreateTable(
                name: "PRODUCTO",
                columns: table => new
                {
                    PRO_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PRO_Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PRO_CALCULOCUOTA = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PRO_TasaReferencial = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PRO_Estado = table.Column<bool>(type: "bit", nullable: true),
                    PRO_Usuario = table.Column<int>(type: "int", nullable: true),
                    PRO_Descripcion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PRODUCTO", x => x.PRO_Id);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudPagoSocio",
                columns: table => new
                {
                    IdSolicitud = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCredito = table.Column<int>(type: "int", nullable: false),
                    IdSocio = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MedioPago = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReferenciaOperacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComprobanteUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdEstado = table.Column<int>(type: "int", nullable: false),
                    ObservacionesCajero = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaProcesamiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdUsuarioCajero = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudPagoSocio", x => x.IdSolicitud);
                    table.ForeignKey(
                        name: "FK_SolicitudPagoSocio_Credito_IdCredito",
                        column: x => x.IdCredito,
                        principalTable: "Credito",
                        principalColumn: "IdCredito",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SolicitudPagoSocio_Socio_IdSocio",
                        column: x => x.IdSocio,
                        principalTable: "Socio",
                        principalColumn: "IdSocio",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConceptosOperacion",
                columns: table => new
                {
                    IdConcepto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoMovimiento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CuentaContableDebe = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    CuentaContableHaber = table.Column<string>(type: "nvarchar(20)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConceptosOperacion", x => x.IdConcepto);
                    table.ForeignKey(
                        name: "FK_ConceptosOperacion_CuentasContables_CuentaContableDebe",
                        column: x => x.CuentaContableDebe,
                        principalTable: "CuentasContables",
                        principalColumn: "CodigoCuenta");
                    table.ForeignKey(
                        name: "FK_ConceptosOperacion_CuentasContables_CuentaContableHaber",
                        column: x => x.CuentaContableHaber,
                        principalTable: "CuentasContables",
                        principalColumn: "CodigoCuenta");
                });

            migrationBuilder.CreateTable(
                name: "AportesSocios",
                columns: table => new
                {
                    IdAporte = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdSocio = table.Column<int>(type: "int", nullable: false),
                    IdConfig = table.Column<int>(type: "int", nullable: false),
                    MesAportado = table.Column<int>(type: "int", nullable: false),
                    AnioAportado = table.Column<int>(type: "int", nullable: false),
                    CantidadAcciones = table.Column<int>(type: "int", nullable: false),
                    MontoPagado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdUsuarioRegistro = table.Column<int>(type: "int", nullable: false),
                    EstadoPago = table.Column<string>(type: "nvarchar(1)", nullable: false),
                    UrlEvidencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComentarioCaja = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaValidacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdUsuarioValidador = table.Column<int>(type: "int", nullable: true),
                    IdMovimientoCaja = table.Column<int>(type: "int", nullable: true),
                    IdMedioPago = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AportesSocios", x => x.IdAporte);
                    table.ForeignKey(
                        name: "FK_AportesSocios_ConfigAportes_IdConfig",
                        column: x => x.IdConfig,
                        principalTable: "ConfigAportes",
                        principalColumn: "IdConfig",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AportesSocios_MediosPago_IdMedioPago",
                        column: x => x.IdMedioPago,
                        principalTable: "MediosPago",
                        principalColumn: "IdMedioPago");
                    table.ForeignKey(
                        name: "FK_AportesSocios_Socio_IdSocio",
                        column: x => x.IdSocio,
                        principalTable: "Socio",
                        principalColumn: "IdSocio",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerfilModulo",
                columns: table => new
                {
                    IdPerfil = table.Column<int>(type: "int", nullable: false),
                    IdModulo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilModulo", x => new { x.IdPerfil, x.IdModulo });
                    table.ForeignKey(
                        name: "FK_PerfilModulo_Modulos_IdModulo",
                        column: x => x.IdModulo,
                        principalTable: "Modulos",
                        principalColumn: "IdModulo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistorialEstadoSocio",
                columns: table => new
                {
                    IdHistorial = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdSocio = table.Column<int>(type: "int", nullable: false),
                    IdUsuarioAccion = table.Column<int>(type: "int", nullable: false),
                    EstadoAnterior = table.Column<bool>(type: "bit", nullable: true),
                    EstadoNuevo = table.Column<bool>(type: "bit", nullable: true),
                    IdMotivo = table.Column<int>(type: "int", nullable: true),
                    Comentario = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaAccion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialEstadoSocio", x => x.IdHistorial);
                    table.ForeignKey(
                        name: "FK_HistorialEstadoSocio_MotivosBaja_IdMotivo",
                        column: x => x.IdMotivo,
                        principalTable: "MotivosBaja",
                        principalColumn: "IdMotivo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialEstadoSocio_Socio_IdSocio",
                        column: x => x.IdSocio,
                        principalTable: "Socio",
                        principalColumn: "IdSocio",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Familiaridad",
                columns: table => new
                {
                    IdFamiliaridad = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdSocioTitular = table.Column<int>(type: "int", nullable: false),
                    IdSocioFamiliar = table.Column<int>(type: "int", nullable: false),
                    IdParentesco = table.Column<int>(type: "int", nullable: false),
                    FechaVinculacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Familiaridad", x => x.IdFamiliaridad);
                    table.ForeignKey(
                        name: "FK_Familiaridad_Parentesco_IdParentesco",
                        column: x => x.IdParentesco,
                        principalTable: "Parentesco",
                        principalColumn: "IdParentesco",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Familiaridad_Socio_IdSocioFamiliar",
                        column: x => x.IdSocioFamiliar,
                        principalTable: "Socio",
                        principalColumn: "IdSocio",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Familiaridad_Socio_IdSocioTitular",
                        column: x => x.IdSocioTitular,
                        principalTable: "Socio",
                        principalColumn: "IdSocio",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Opciones",
                columns: table => new
                {
                    IdOpcion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Icono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Modulo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    PerfilIdPerfil = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Opciones", x => x.IdOpcion);
                    table.ForeignKey(
                        name: "FK_Opciones_Perfiles_PerfilIdPerfil",
                        column: x => x.PerfilIdPerfil,
                        principalTable: "Perfiles",
                        principalColumn: "IdPerfil");
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    PasswordSalt = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreCompleto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdPerfil = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    IntentosFallidos = table.Column<int>(type: "int", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    UltimoLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UltimoAcceso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdSocio = table.Column<int>(type: "int", nullable: true),
                    RequiereCambioPassword = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.IdUsuario);
                    table.ForeignKey(
                        name: "FK_Usuarios_Perfiles_IdPerfil",
                        column: x => x.IdPerfil,
                        principalTable: "Perfiles",
                        principalColumn: "IdPerfil",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Usuarios_Socio_IdSocio",
                        column: x => x.IdSocio,
                        principalTable: "Socio",
                        principalColumn: "IdSocio");
                });

            migrationBuilder.CreateTable(
                name: "ProductoTasas",
                columns: table => new
                {
                    IdTasa = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PRO_Id = table.Column<int>(type: "int", nullable: false),
                    MontoMinimo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoMaximo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TasaInteres = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PROTA_Usuario = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductoTasas", x => x.IdTasa);
                    table.ForeignKey(
                        name: "FK_ProductoTasas_PRODUCTO_PRO_Id",
                        column: x => x.PRO_Id,
                        principalTable: "PRODUCTO",
                        principalColumn: "PRO_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Solicitudes",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SocioId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    MontoSolicitado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PlazoSolicitado = table.Column<int>(type: "int", nullable: false),
                    TasaPropuesta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioCreadorId = table.Column<int>(type: "int", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsuarioEvaluador = table.Column<int>(type: "int", nullable: true),
                    FechaEvaluacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ComentarioEvaluador = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solicitudes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Solicitudes_PRODUCTO_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "PRODUCTO",
                        principalColumn: "PRO_Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Solicitudes_Socio_SocioId",
                        column: x => x.SocioId,
                        principalTable: "Socio",
                        principalColumn: "IdSocio",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudPagoDetalle",
                columns: table => new
                {
                    IdSolicitudDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdSolicitud = table.Column<int>(type: "int", nullable: false),
                    IdCuota = table.Column<int>(type: "int", nullable: false),
                    MontoAplicado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InteresCubierto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CapitalCubierto = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudPagoDetalle", x => x.IdSolicitudDetalle);
                    table.ForeignKey(
                        name: "FK_SolicitudPagoDetalle_Cuota_IdCuota",
                        column: x => x.IdCuota,
                        principalTable: "Cuota",
                        principalColumn: "IdCuota");
                    table.ForeignKey(
                        name: "FK_SolicitudPagoDetalle_SolicitudPagoSocio_IdSolicitud",
                        column: x => x.IdSolicitud,
                        principalTable: "SolicitudPagoSocio",
                        principalColumn: "IdSolicitud");
                });

            migrationBuilder.CreateTable(
                name: "MovimientosCaja",
                columns: table => new
                {
                    IdMovimiento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdConcepto = table.Column<int>(type: "int", nullable: false),
                    IdCredito = table.Column<int>(type: "int", nullable: true),
                    IdAsiento = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    IdCaja = table.Column<int>(type: "int", nullable: true),
                    IdMedioPago = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosCaja", x => x.IdMovimiento);
                    table.ForeignKey(
                        name: "FK_MovimientosCaja_ConceptosOperacion_IdConcepto",
                        column: x => x.IdConcepto,
                        principalTable: "ConceptosOperacion",
                        principalColumn: "IdConcepto",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Credito_IdSocio",
                table: "Credito",
                column: "IdSocio");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_IdCredito",
                table: "Pagos",
                column: "IdCredito");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_IdMedioPago",
                table: "Pagos",
                column: "IdMedioPago");

            migrationBuilder.CreateIndex(
                name: "IX_AportesSocios_IdConfig",
                table: "AportesSocios",
                column: "IdConfig");

            migrationBuilder.CreateIndex(
                name: "IX_AportesSocios_IdMedioPago",
                table: "AportesSocios",
                column: "IdMedioPago");

            migrationBuilder.CreateIndex(
                name: "IX_AportesSocios_IdSocio_MesAportado_AnioAportado",
                table: "AportesSocios",
                columns: new[] { "IdSocio", "MesAportado", "AnioAportado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConceptosOperacion_CuentaContableDebe",
                table: "ConceptosOperacion",
                column: "CuentaContableDebe");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptosOperacion_CuentaContableHaber",
                table: "ConceptosOperacion",
                column: "CuentaContableHaber");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleAsiento_IdAsiento",
                table: "DetalleAsiento",
                column: "IdAsiento");

            migrationBuilder.CreateIndex(
                name: "IX_Familiaridad_IdParentesco",
                table: "Familiaridad",
                column: "IdParentesco");

            migrationBuilder.CreateIndex(
                name: "IX_Familiaridad_IdSocioFamiliar",
                table: "Familiaridad",
                column: "IdSocioFamiliar");

            migrationBuilder.CreateIndex(
                name: "IX_Familiaridad_IdSocioTitular",
                table: "Familiaridad",
                column: "IdSocioTitular");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstadoSocio_IdMotivo",
                table: "HistorialEstadoSocio",
                column: "IdMotivo");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstadoSocio_IdSocio",
                table: "HistorialEstadoSocio",
                column: "IdSocio");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_IdConcepto",
                table: "MovimientosCaja",
                column: "IdConcepto");

            migrationBuilder.CreateIndex(
                name: "IX_Opciones_PerfilIdPerfil",
                table: "Opciones",
                column: "PerfilIdPerfil");

            migrationBuilder.CreateIndex(
                name: "IX_PagosDetalle_IdCuota",
                table: "PagosDetalle",
                column: "IdCuota");

            migrationBuilder.CreateIndex(
                name: "IX_PagosDetalle_IdPago",
                table: "PagosDetalle",
                column: "IdPago");

            migrationBuilder.CreateIndex(
                name: "IX_PagosDetalle_MoraIdMora",
                table: "PagosDetalle",
                column: "MoraIdMora");

            migrationBuilder.CreateIndex(
                name: "IX_PerfilModulo_IdModulo",
                table: "PerfilModulo",
                column: "IdModulo");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoTasas_PRO_Id",
                table: "ProductoTasas",
                column: "PRO_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_ProductoId",
                schema: "dbo",
                table: "Solicitudes",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_SocioId",
                schema: "dbo",
                table: "Solicitudes",
                column: "SocioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudPagoDetalle_IdCuota",
                table: "SolicitudPagoDetalle",
                column: "IdCuota");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudPagoDetalle_IdSolicitud",
                table: "SolicitudPagoDetalle",
                column: "IdSolicitud");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudPagoSocio_IdCredito",
                table: "SolicitudPagoSocio",
                column: "IdCredito");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudPagoSocio_IdSocio",
                table: "SolicitudPagoSocio",
                column: "IdSocio");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_IdPerfil",
                table: "Usuarios",
                column: "IdPerfil");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_IdSocio",
                table: "Usuarios",
                column: "IdSocio");

            migrationBuilder.AddForeignKey(
                name: "FK_Credito_Socio_IdSocio",
                table: "Credito",
                column: "IdSocio",
                principalTable: "Socio",
                principalColumn: "IdSocio",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pagos_Credito_IdCredito",
                table: "Pagos",
                column: "IdCredito",
                principalTable: "Credito",
                principalColumn: "IdCredito");

            migrationBuilder.AddForeignKey(
                name: "FK_Pagos_MediosPago_IdMedioPago",
                table: "Pagos",
                column: "IdMedioPago",
                principalTable: "MediosPago",
                principalColumn: "IdMedioPago");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Credito_Socio_IdSocio",
                table: "Credito");

            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_Credito_IdCredito",
                table: "Pagos");

            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_MediosPago_IdMedioPago",
                table: "Pagos");

            migrationBuilder.DropTable(
                name: "AportesSocios");

            migrationBuilder.DropTable(
                name: "AprobacionResponse");

            migrationBuilder.DropTable(
                name: "DetalleAsiento");

            migrationBuilder.DropTable(
                name: "Familiaridad");

            migrationBuilder.DropTable(
                name: "GlobalSettings");

            migrationBuilder.DropTable(
                name: "HistorialEstadoSocio");

            migrationBuilder.DropTable(
                name: "Logs_Actividad");

            migrationBuilder.DropTable(
                name: "MovimientosCaja");

            migrationBuilder.DropTable(
                name: "Opciones");

            migrationBuilder.DropTable(
                name: "OperacionResponses");

            migrationBuilder.DropTable(
                name: "PagosDetalle");

            migrationBuilder.DropTable(
                name: "PerfilModulo");

            migrationBuilder.DropTable(
                name: "ProductoTasas");

            migrationBuilder.DropTable(
                name: "Solicitudes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SolicitudPagoDetalle");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "ConfigAportes");

            migrationBuilder.DropTable(
                name: "MediosPago");

            migrationBuilder.DropTable(
                name: "Parentesco");

            migrationBuilder.DropTable(
                name: "MotivosBaja");

            migrationBuilder.DropTable(
                name: "ConceptosOperacion");

            migrationBuilder.DropTable(
                name: "Modulos");

            migrationBuilder.DropTable(
                name: "PRODUCTO");

            migrationBuilder.DropTable(
                name: "SolicitudPagoSocio");

            migrationBuilder.DropTable(
                name: "Perfiles");

            migrationBuilder.DropTable(
                name: "CuentasContables");

            migrationBuilder.DropIndex(
                name: "IX_Credito_IdSocio",
                table: "Credito");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Pagos",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_Pagos_IdCredito",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_Pagos_IdMedioPago",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "ApellidoMaterno",
                table: "Socio");

            migrationBuilder.DropColumn(
                name: "ApellidoPaterno",
                table: "Socio");

            migrationBuilder.DropColumn(
                name: "FechaModificacion",
                table: "Socio");

            migrationBuilder.DropColumn(
                name: "FechaNacimiento",
                table: "Socio");

            migrationBuilder.DropColumn(
                name: "IdUsuarioModificacion",
                table: "Socio");

            migrationBuilder.DropColumn(
                name: "IdUsuarioRegistro",
                table: "Socio");

            migrationBuilder.DropColumn(
                name: "correo",
                table: "Socio");

            migrationBuilder.DropColumn(
                name: "EstadoCredito",
                table: "Credito");

            migrationBuilder.DropColumn(
                name: "FechaDesembolso",
                table: "Credito");

            migrationBuilder.DropColumn(
                name: "MontoDesembolsado",
                table: "Credito");

            migrationBuilder.DropColumn(
                name: "IdReferencia",
                table: "AsientosContables");

            migrationBuilder.DropColumn(
                name: "IdMedioPago",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "IdSocio",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "IdUsuario",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "NroOperacion",
                table: "Pagos");

            migrationBuilder.RenameTable(
                name: "Pagos",
                newName: "Pago");

            migrationBuilder.RenameColumn(
                name: "Origen",
                table: "AsientosContables",
                newName: "TipoOperacion");

            migrationBuilder.RenameColumn(
                name: "Glosa",
                table: "AsientosContables",
                newName: "Estado");

            migrationBuilder.RenameColumn(
                name: "MontoTotal",
                table: "Pago",
                newName: "MontoPagado");

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "Socio",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaRegistro",
                table: "Socio",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Direccion",
                table: "Socio",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DNI",
                table: "Socio",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(8)",
                oldMaxLength: 8);

            migrationBuilder.AddColumn<string>(
                name: "CuentaDebe",
                table: "AsientosContables",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CuentaHaber",
                table: "AsientosContables",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Monto",
                table: "AsientosContables",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ReferenciaId",
                table: "AsientosContables",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Pago",
                table: "Pago",
                column: "IdPago");

            migrationBuilder.CreateTable(
                name: "DetallePago",
                columns: table => new
                {
                    IdDetallePago = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCuota = table.Column<int>(type: "int", nullable: true),
                    IdMora = table.Column<int>(type: "int", nullable: true),
                    IdPago = table.Column<int>(type: "int", nullable: false),
                    CapitalPagado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InteresPagado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MoraPagada = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallePago", x => x.IdDetallePago);
                    table.ForeignKey(
                        name: "FK_DetallePago_Cuota_IdCuota",
                        column: x => x.IdCuota,
                        principalTable: "Cuota",
                        principalColumn: "IdCuota");
                    table.ForeignKey(
                        name: "FK_DetallePago_Moras_IdMora",
                        column: x => x.IdMora,
                        principalTable: "Moras",
                        principalColumn: "IdMora");
                    table.ForeignKey(
                        name: "FK_DetallePago_Pago_IdPago",
                        column: x => x.IdPago,
                        principalTable: "Pago",
                        principalColumn: "IdPago",
                        onDelete: ReferentialAction.Cascade);
                });

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
        }
    }
}

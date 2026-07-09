using CooperativaApp.Data;
using CooperativaApp.DTOs;
using CooperativaApp.DTOS;
using CooperativaApp.Interfaces;
using CooperativaApp.Models;
using CooperativaApp.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace CooperativaApp.Services.Implementations
{
    public class UtilidadService : IUtilidadService
    {
        private readonly CooperativaContext _context;

        public UtilidadService(CooperativaContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<bool> VerificarPeriodoProcesadoAsync(int mes, int anio)
        {
            return await _context.UtilidadesProcesadas.AnyAsync(x => x.Mes == mes && x.Anio == anio);
        }

        public async Task<bool> ValidarEstadoPeriodoConfigAsync(int idPeriodoConfig)
        {
            var periodo = await _context.PeriodosRetiroUtilidad.FirstOrDefaultAsync(p => p.IdPeriodoConfig == idPeriodoConfig);
            return periodo != null && (periodo.Estado.ToUpper() == "HABILITADO" || periodo.Estado.ToUpper() == "CONFIGURADO");
        }

        public async Task EjecutarAlgoritmoProrrateoAsync(int idPeriodoConfig, int mes, int anio, int idUsuario)
        {
            var paramPeriodo = new SqlParameter("@IdPeriodoConfig", SqlDbType.Int) { Value = idPeriodoConfig };
            var paramMes = new SqlParameter("@MesEvaluar", SqlDbType.Int) { Value = mes };
            var paramAnio = new SqlParameter("@AnioEvaluar", SqlDbType.Int) { Value = anio };
            var paramUsuario = new SqlParameter("@IdUsuarioRegistro", SqlDbType.Int) { Value = idUsuario };
            var paramSimulacion = new SqlParameter("@Simulacion", SqlDbType.Bit) { Value = 0 }; // Impacto contable directo

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.USP_ProcesarUtilidadesMensuales @IdPeriodoConfig, @MesEvaluar, @AnioEvaluar, @IdUsuarioRegistro, @Simulacion",
                paramPeriodo, paramMes, paramAnio, paramUsuario, paramSimulacion
            );
        }

        public async Task<decimal> ObtenerSaldoDisponibleAsync(int idSocio, int idPeriodoConfig)
        {
            // 🎯 Agregamos la coalescencia (?? 0m) para convertir de 'decimal?' a 'decimal' de forma segura
            return await _context.UtilidadesProcesadas
                .Where(u => u.IdSocio == idSocio && u.IdPeriodoConfig == idPeriodoConfig)
                .SumAsync(u => u.MontoDisponible) ?? 0m;
        }

        public async Task<PeriodosRetiroUtilidad?> ObtenerPeriodoActivoAsync()
        {
            return await _context.PeriodosRetiroUtilidad
                .FirstOrDefaultAsync(p => p.Estado.ToUpper() == "PROCESADO" || p.Estado.ToUpper() == "HABILITADO");
        }

        public async Task RegistrarSolicitudRetiroAsync(int idSocio, int idPeriodoConfig, decimal monto)
        {
            var disponible = await ObtenerSaldoDisponibleAsync(idSocio, idPeriodoConfig);
            string tipo = monto == disponible ? "TOTAL" : "PARCIAL";

            var solicitud = new SolicitudUtilidad
            {
                IdSocio = idSocio,
                IdPeriodoConfig = idPeriodoConfig,
                MontoSolicitado = monto,
                TipoRetiro = tipo,
                Estado = "PENDIENTE",
                FechaSolicitud = DateTime.Now
            };

            _context.SolicitudesUtilidad.Add(solicitud);
            await _context.SaveChangesAsync();
        }

        public async Task<List<PeriodosRetiroUtilidad>> ListarPeriodosConfiguracionAsync()
        {
            return await _context.PeriodosRetiroUtilidad.OrderByDescending(p => p.IdPeriodoConfig).ToListAsync();
        }

        public async Task RegistrarPeriodoConfiguracionAsync(PeriodosRetiroUtilidad periodo)
        {
            _context.PeriodosRetiroUtilidad.Add(periodo);
            await _context.SaveChangesAsync();
        }

        // ── 🎯 IMPLEMENTACIÓN EXTRA GALÁCTICA PARA EXTRAER EL DATA TABLE DE LA SIMULACIÓN ──
        public async Task<DataTable> SimularProrrateoMensualAsync(int idPeriodoConfig, int mes, int anio)
        {
            var dt = new DataTable();
            var connection = _context.Database.GetDbConnection();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "dbo.USP_ProcesarUtilidadesMensuales";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@IdPeriodoConfig", SqlDbType.Int) { Value = idPeriodoConfig });
                cmd.Parameters.Add(new SqlParameter("@MesEvaluar", SqlDbType.Int) { Value = mes });
                cmd.Parameters.Add(new SqlParameter("@AnioEvaluar", SqlDbType.Int) { Value = anio });
                cmd.Parameters.Add(new SqlParameter("@IdUsuarioRegistro", SqlDbType.Int) { Value = 0 });
                cmd.Parameters.Add(new SqlParameter("@Simulacion", SqlDbType.Bit) { Value = 1 }); // Activamos simulación

                if (connection.State == ConnectionState.Closed) await connection.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    dt.Load(reader);
                }
            }
            return dt;
        }// ── 🎯 LA SOLUCIÓN REFACTORIZADA HISTÓRICA CON CONVERSIÓN EXPLICITA DE TIPOS ──
        public async Task<IEnumerable<HistorialUtilidadDto>> ObtenerHistorialProcesadoAsync(int idPeriodoConfig, int mes, int anio)
        {
            var historial = await _context.UtilidadesProcesadas
                .Where(u => u.IdPeriodoConfig == idPeriodoConfig && u.Anio == anio && u.Mes <= mes)
                .OrderBy(u => u.IdSocio).ThenBy(u => u.Anio).ThenBy(u => u.Mes)
                .Select(u => new HistorialUtilidadDto
                {
                    PeriodoNombre = _context.PeriodosRetiroUtilidad
                        .Where(p => p.IdPeriodoConfig == idPeriodoConfig)
                        .Select(p => p.NombrePeriodo).FirstOrDefault() ?? "UTILIDAD",
                    MesEvaluado = u.Mes == 1 ? "ENERO" : u.Mes == 2 ? "FEBRERO" : u.Mes == 3 ? "MARZO" :
                                  u.Mes == 4 ? "ABRIL" : u.Mes == 5 ? "MAYO" : u.Mes == 6 ? "JUNIO" :
                                  u.Mes == 7 ? "JULIO" : "DICIEMBRE",
                    AnioFiscal = u.Anio,
                    // 🎯 FIX: Convertimos de decimal? a decimal usando coalescencia ?? 0m
                    InteresMensualBruto = u.InteresMensualRepartir ?? 0m,
                    GastoMensual = 0.00m,
                    TotalAportesConsolidado = _context.UtilidadesProcesadas
                        .Where(up => up.IdPeriodoConfig == idPeriodoConfig && up.Mes == u.Mes && up.Anio == u.Anio)
                        .Sum(up => up.AporteAcumuladoMes) ?? 0m,
                    TotalUtilidadConsolidada = _context.UtilidadesProcesadas
                        .Where(up => up.IdPeriodoConfig == idPeriodoConfig && up.Mes == u.Mes && up.Anio == u.Anio)
                        .Sum(up => up.UtilidadObtenida) ?? 0m,
                    IdSocio = u.IdSocio,
                    CodigoSocio = u.IdSocio.ToString(),
                    // 🎯 FIX: Ajustado el acceso al DbSet del núcleo de socios de acuerdo a tu contexto de EF
                    NombreCompleto = _context.UtilidadesProcesadas
                        .Include(x => x.PeriodoConfig) // O cámbialo por tu consulta directa cruzada
                        .Where(x => x.IdSocio == u.IdSocio)
                        .Select(x => "Socio " + u.IdSocio).FirstOrDefault() ?? "Socio Activo",
                    AporteAcumulado = u.AporteAcumuladoMes ?? 0m,
                    AporteDelMes = 0.00m,
                    UtilidadGenerada = u.UtilidadObtenida ?? 0m,
                    AporteAcumuladoFinal = u.AporteAcumuladoFinal ?? 0m
                })
                .ToListAsync();

            return historial;
        }
    }
}
using CooperativaApp.Data;
using CooperativaApp.Models;
using CooperativaApp.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
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
            // Ahora usando Linq directo gracias al DbSet del DbContext
            return await _context.UtilidadesProcesadas
                .AnyAsync(x => x.Mes == mes && x.Anio == anio);
        }

        public async Task<bool> ValidarEstadoPeriodoConfigAsync(int idPeriodoConfig)
        {
            // Usando Linq directo sobre el periodo de retiro
            var periodo = await _context.PeriodosRetiroUtilidad
                .FirstOrDefaultAsync(p => p.IdPeriodoConfig == idPeriodoConfig);

            return periodo != null && periodo.Estado.ToUpper() == "HABILITADO";
        }

        public async Task EjecutarAlgoritmoProrrateoAsync(int idPeriodoConfig, int mes, int anio, int idUsuario)
        {
            var paramPeriodo = new SqlParameter("@IdPeriodoConfig", SqlDbType.Int) { Value = idPeriodoConfig };
            var paramMes = new SqlParameter("@MesEvaluar", SqlDbType.Int) { Value = mes };
            var paramAnio = new SqlParameter("@AnioEvaluar", SqlDbType.Int) { Value = anio };
            var paramUsuario = new SqlParameter("@IdUsuarioRegistro", SqlDbType.Int) { Value = idUsuario };

            // Invocación segura del Stored Procedure
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.USP_ProcesarUtilidadesMensuales @IdPeriodoConfig, @MesEvaluar, @AnioEvaluar, @IdUsuarioRegistro",
                paramPeriodo, paramMes, paramAnio, paramUsuario
            );
        }
        public async Task<decimal> ObtenerSaldoDisponibleAsync(int idSocio, int idPeriodoConfig)
        {
            return await _context.UtilidadesProcesadas
                .Where(u => u.IdSocio == idSocio && u.IdPeriodoConfig == idPeriodoConfig)
                .SumAsync(u => u.MontoDisponible);
        }

        public async Task<PeriodosRetiroUtilidad?> ObtenerPeriodoActivoAsync()
        {
            return await _context.PeriodosRetiroUtilidad
                .FirstOrDefaultAsync(p => p.Estado.ToUpper() == "PROCESADO" || p.Estado.ToUpper() == "HABILITADO");
        }

        public async Task RegistrarSolicitudRetiroAsync(int idSocio, int idPeriodoConfig, decimal monto)
        {
            // Calculamos si es total o parcial comparando el monto solicitado contra el total disponible
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
    }
}
using CooperativaApp.DTOs;
using CooperativaApp.Models;
using System.Data;
using System.Threading.Tasks;

namespace CooperativaApp.Services.Interfaces
{
    public interface IUtilidadService
    {
        Task<bool> VerificarPeriodoProcesadoAsync(int mes, int anio);
        Task<bool> ValidarEstadoPeriodoConfigAsync(int idPeriodoConfig);
        Task EjecutarAlgoritmoProrrateoAsync(int idPeriodoConfig, int mes, int anio, int idUsuario);
        Task<decimal> ObtenerSaldoDisponibleAsync(int idSocio, int idPeriodoConfig);
        Task<PeriodosRetiroUtilidad?> ObtenerPeriodoActivoAsync();
        Task RegistrarSolicitudRetiroAsync(int idSocio, int idPeriodoConfig, decimal monto);

        Task<List<PeriodosRetiroUtilidad>> ListarPeriodosConfiguracionAsync();
        Task RegistrarPeriodoConfiguracionAsync(PeriodosRetiroUtilidad periodo);
        // 🚀 ADICIÓN DIAMANTE: Método para invocar la previsualización dinámica desde un DataSet/DataTable
        Task<DataTable> SimularProrrateoMensualAsync(int idPeriodoConfig, int mes, int anio);
        
        // 🚀 FIRMA ALINEADA TOP DIAMANTE
        Task<IEnumerable<HistorialUtilidadDto>> ObtenerHistorialProcesadoAsync(int idPeriodoConfig, int mes, int anio);

    }
}
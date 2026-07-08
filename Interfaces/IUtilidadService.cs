using CooperativaApp.Models;
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
    }
}
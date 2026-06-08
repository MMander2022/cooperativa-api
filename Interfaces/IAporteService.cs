using CooperativaApp.Models;
using System.Threading.Tasks;
using CooperativaApp.DTOs;

namespace CooperativaApp.Interfaces
{
    public interface IAporteService
    {
        Task<ConfigAporte> GetConfiguracionVigenteAsync();
        Task<(bool Success, string Message)> RegistrarAporteAsync(AporteSocio aporte);
        Task<decimal> GetTotalAcumuladoAnualAsync(int idSocio, int anio);
        Task<ReporteAportesConsolidadoDto> ObtenerReporteConsolidadoAsync(int idSocioToken, int idUsuarioToken, bool esAdmin, int? anioConsulta);

    }
}

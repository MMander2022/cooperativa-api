using CooperativaApp.Models;

namespace CooperativaApp.Interfaces
{
    public interface IAporteService
    {
        Task<ConfigAporte> GetConfiguracionVigenteAsync();
        Task<(bool Success, string Message)> RegistrarAporteAsync(AporteSocio aporte);
        Task<decimal> GetTotalAcumuladoAnualAsync(int idSocio, int anio);
    }
}

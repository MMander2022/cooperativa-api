using CooperativaApp.Models;
using static CooperativaApp.Controllers.CreditosController;
using CooperativaApp.DTOS;
namespace CooperativaApp.Interfaces
{
    public interface ICreditoService
    {
        Task<string> DesembolsarCreditoAsync(int idCredito, int idUsuario);
        Task<IEnumerable<object>> ObtenerCreditosPorSocioAsync(int socioId);
        Task<IEnumerable<CuotaDetalleDTO>> ObtenerPlanPagosAsync(int idCredito);
        Task<OperacionResponse> RegistrarDesembolsoAsync(DesembolsoRequest request);
        Task<IEnumerable<CreditoSocioDTO>> ObtenerCreditosPorPerfilAsync(int usuarioId, string perfil, int? socioId);
        Task<List<CuotaDetalleDTO>> GetPlanPagosConAuditoriaAsync(int idCredito);
        Task<List<CuotaAnaliticaDTO>> GetPlanPagosAnaliticoAsync(int idCredito);
        

    }
}

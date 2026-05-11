using CooperativaApp.DTOS;

namespace CooperativaApp.Interfaces
{
    public interface ISolicitudPagoService
    {
        Task<IEnumerable<object>> ObtenerPendientesAsync();
        Task<OperacionResponse> CrearSolicitudSocioAsync(RegistrarSolicitudPagoDTO dto, string perfil, int? socioId);

        Task<OperacionResponse> ProcesarSolicitudAsync(int idSolicitud, string accion, string motivo, int usuarioId);
    }
}

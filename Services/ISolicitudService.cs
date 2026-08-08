using CooperativaApp.DTOs;
using CooperativaApp.DTOS;
using CooperativaApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CooperativaApp.Services
{
    public interface ISolicitudService
    {

       // Task<int> RegistrarSolicitudAsync(SolicitudCreateDTO dto);

        // Ajuste: Nombre de parámetro consistente con la implementación
        Task<IEnumerable<SolicitudPendienteDTO>> ObtenerPendientesAsync(decimal montoMaximoAutorizado);

        Task<int> RegistrarSolicitudAsync(SolicitudCreateDTO dto, int usuarioId);
        Task<AprobacionResponse> AprobarConSPAsync(int solicitudId, int usuarioId, string comentario);
        Task<AprobacionResponse> DecidirSolicitudAsync(int solicitudId, DecisionRequestDTO request);
        Task<IEnumerable<SolicitudDetalleDTO>> ListarTodasAsync();
        Task<IEnumerable<SolicitudDetalleDTO>> ObtenerPorSocioAsync(int socioId);
        // 🎯 NUEVO:
        Task<object> ObtenerAnalisisRiesgoSocioAsync(int idSocio);
        Task<object> ValidarSocioAvalAsync(string dni);
    }
}

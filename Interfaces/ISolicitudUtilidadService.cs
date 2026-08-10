using CooperativaApp.DTOs;
using CooperativaApp.DTOS;
using CooperativaApp.Models;
using CooperativaDB.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CooperativaApp.Services.Interfaces
{
    public interface ISolicitudUtilidadService
    {
        Task<ResumenSocioUtilidadDto> ObtenerResumenRetiroSocioAsync(int idSocio);
        Task<IEnumerable<UtilidadesProcesadas>> ObtenerDetalleMensualSocioAsync(int idSocio);
        Task RegistrarSolicitudAsync(SolicitudUtilidadDto dto);
        Task ModificarSolicitudAsync(int idSolicitud, decimal nuevoMonto);
        Task EliminarSolicitudLogicAsync(int idSolicitud);
        Task<IEnumerable<SolicitudUtilidad>> ListarSolicitudesPorEstadoAsync(string estado);
        Task<IEnumerable<SolicitudPendienteCajaDto>> ListarSolicitudesPendientesOrdenadasAsync();
        Task ProcesarDesembolsoCajaAsync(DesembolsoPayloadDto desembolso);
        Task RechazarSolicitudCajaAsync(int idSolicitud, int idUsuario, string comentario);
        // 🎯 NUEVOS MÉTODOS PARA CARGA MASIVA (ADMIN)
        Task<List<SocioHabilitadoUtilidadDto>> ObtenerSociosHabilitadosPeriodoAsync(int idPeriodoConfig);
        Task ProcesarSolicitudesMasivasAsync(SolicitudMasivaPayloadDto payload);
    }
}
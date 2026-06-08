using System.Collections.Generic;
using System.Threading.Tasks;
using CooperativaApp.DTOs;
using CooperativaApp.Models;

namespace CooperativaApp.Services
{
    public interface IRetiroService
    {
        Task<PeriodoRetiro> ObtenerPeriodoActivoVentanillaAsync();
        Task<List<RetiroItemResponse>> ListarMisSolicitudesAsync(int idSocio);
        Task<List<RetiroItemResponse>> ListarPendientesCajaAsync();
        Task<(bool Success, string Message)> RegistrarSolicitudAsync(int idSocio, SolicitudRetiroDto dto);
        Task<(bool Success, string Message)> ModificarSolicitudAsync(int idSolicitud, int idSocio, decimal nuevoMonto);
        Task<(bool Success, string Message)> AnularSolicitudAsync(int idSolicitud, int idSocio);
        Task<(bool Success, string Message)> EvaluarSolicitudCajaAsync(EvaluacionRetiroDto dto);
    }
}
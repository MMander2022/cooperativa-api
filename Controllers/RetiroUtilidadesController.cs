using CooperativaApp.DTOs;
using CooperativaApp.DTOS;
using CooperativaApp.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CooperativaApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RetiroUtilidadesController : ControllerBase
    {
        private readonly ISolicitudUtilidadService _service;

        public RetiroUtilidadesController(ISolicitudUtilidadService service)
        {
            _service = service;
        }

        // 🎯 1. Resumen de Saldo y Ventana de Retiro del Socio
        [HttpGet("resumen-socio/{idSocio}")]
        public async Task<IActionResult> GetResumenSocio(int idSocio)
            => Ok(await _service.ObtenerResumenRetiroSocioAsync(idSocio));

        // 🎯 2. Historial de Rendimientos Mensuales Procesados
        [HttpGet("detalle-mensual/{idSocio}")]
        public async Task<IActionResult> GetDetalleMensual(int idSocio)
            => Ok(await _service.ObtenerDetalleMensualSocioAsync(idSocio));

        // 🎯 3. Registrar Nueva Solicitud de Retiro
        [HttpPost("solicitar")]
        public async Task<IActionResult> Registrar([FromBody] SolicitudUtilidadDto dto)
        {
            try
            {
                await _service.RegistrarSolicitudAsync(dto);
                return Ok(new { Message = "Solicitud archivada con éxito." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // 🎯 4. Rectificar Importe de Solicitud Pendiente
        [HttpPut("modificar/{idSolicitud}")]
        public async Task<IActionResult> Modificar(int idSolicitud, [FromQuery] decimal monto)
        {
            try
            {
                await _service.ModificarSolicitudAsync(idSolicitud, monto);
                return Ok(new { Message = "Solicitud rectificada correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // 🎯 5. Anular / Dar de baja Solicitud Pendiente
        [HttpDelete("eliminar/{idSolicitud}")]
        public async Task<IActionResult> Eliminar(int idSolicitud)
        {
            try
            {
                await _service.EliminarSolicitudLogicAsync(idSolicitud);
                return Ok(new { Message = "Solicitud dada de baja." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // 🎯 6. Lista de Solicitudes Pendientes Generales
        [HttpGet("pendientes")]
        public async Task<IActionResult> ListarPendientes()
            => Ok(await _service.ListarSolicitudesPorEstadoAsync("PENDIENTE"));

        // 🎯 7. NUEVO: Lista de Pendientes para Caja (Ordenada por Apellido Paterno)
        [HttpGet("pendientes-ordenadas")]
        public async Task<IActionResult> ListarPendientesOrdenadas()
            => Ok(await _service.ListarSolicitudesPendientesOrdenadasAsync());

        // 🎯 8. Procesar Desembolso en Ventanilla Caja
        [HttpPost("caja/desembolsar")]
        public async Task<IActionResult> Desembolsar([FromBody] DesembolsoPayloadDto payload)
        {
            try
            {
                await _service.ProcesarDesembolsoCajaAsync(payload);
                return Ok(new { Message = "Desembolso procesado en libro de caja con éxito." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // 🎯 9. NUEVO: Rechazar Solicitud en Caja con Sustento
        [HttpPost("caja/rechazar")]
        public async Task<IActionResult> Rechazar([FromQuery] int idSolicitud, [FromQuery] int idUsuario, [FromQuery] string comentario)
        {
            try
            {
                await _service.RechazarSolicitudCajaAsync(idSolicitud, idUsuario, comentario);
                return Ok(new { Message = "Solicitud de utilidad rechazada correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
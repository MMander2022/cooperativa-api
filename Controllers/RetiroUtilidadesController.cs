using CooperativaApp.DTOs;
using CooperativaApp.DTOS;
using CooperativaApp.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
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

        [HttpGet("resumen-socio/{idSocio}")]
        public async Task<IActionResult> GetResumenSocio(int idSocio) => Ok(await _service.ObtenerResumenRetiroSocioAsync(idSocio));

        [HttpGet("detalle-mensual/{idSocio}")]
        public async Task<IActionResult> GetDetalleMensual(int idSocio) => Ok(await _service.ObtenerDetalleMensualSocioAsync(idSocio));

        [HttpPost("solicitar")]
        public async Task<IActionResult> Registrar([FromBody] SolicitudUtilidadDto dto)
        {
            await _service.RegistrarSolicitudAsync(dto);
            return Ok(new { Message = "Solicitud archivada con éxito." });
        }

        [HttpPut("modificar/{idSolicitud}")]
        public async Task<IActionResult> Modificar(int idSolicitud, [FromQuery] decimal monto)
        {
            await _service.ModificarSolicitudAsync(idSolicitud, monto);
            return Ok(new { Message = "Solicitud rectificada correctamente." });
        }

        [HttpDelete("eliminar/{idSolicitud}")]
        public async Task<IActionResult> Eliminar(int idSolicitud)
        {
            await _service.EliminarSolicitudLogicAsync(idSolicitud);
            return Ok(new { Message = "Solicitud dada de baja." });
        }

        [HttpGet("pendientes")]
        public async Task<IActionResult> ListarPendientes() => Ok(await _service.ListarSolicitudesPorEstadoAsync("PENDIENTE"));

        [HttpPost("caja/desembolsar")]
        public async Task<IActionResult> Desembolsar([FromBody] DesembolsoPayloadDto payload)
        {
            await _service.ProcesarDesembolsoCajaAsync(payload);
            return Ok(new { Message = "Desembolso procesado en libro de caja con éxito." });
        }
    }
}
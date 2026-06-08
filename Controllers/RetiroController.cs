using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CooperativaApp.Services;
using CooperativaApp.DTOs;

namespace CooperativaApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RetiroController : ControllerBase
    {
        private readonly IRetiroService _retiroService;

        public RetiroController(IRetiroService retiroService)
        {
            _retiroService = retiroService;
        }

        [HttpGet("periodo-vigente")]
        public async Task<IActionResult> GetPeriodo() => Ok(await _retiroService.ObtenerPeriodoActivoVentanillaAsync());

        [HttpGet("mis-solicitudes/{idSocio}")]
        public async Task<IActionResult> GetMisSolicitudes(int idSocio) => Ok(await _retiroService.ListarMisSolicitudesAsync(idSocio));

        [HttpGet("pendientes-caja")]
        public async Task<IActionResult> GetPendientes() => Ok(await _retiroService.ListarPendientesCajaAsync());

        [HttpPost("solicitar/{idSocio}")]
        public async Task<IActionResult> Registrar(int idSocio, [FromBody] SolicitudRetiroDto dto)
        {
            var r = await _retiroService.RegistrarSolicitudAsync(idSocio, dto);
            if (!r.Success) return BadRequest(new { message = r.Message });
            return Ok(new { message = r.Message });
        }

        [HttpPut("modificar/{idSolicitud}/{idSocio}")]
        public async Task<IActionResult> Modificar(int idSolicitud, int idSocio, [FromBody] decimal monto)
        {
            var r = await _retiroService.ModificarSolicitudAsync(idSolicitud, idSocio, monto);
            if (!r.Success) return BadRequest(new { message = r.Message });
            return Ok(new { message = r.Message });
        }

        [HttpPut("anular/{idSolicitud}/{idSocio}")]
        public async Task<IActionResult> Anular(int idSolicitud, int idSocio)
        {
            var r = await _retiroService.AnularSolicitudAsync(idSolicitud, idSocio);
            if (!r.Success) return BadRequest(new { message = r.Message });
            return Ok(new { message = r.Message });
        }

        [HttpPost("evaluar-caja")]
        public async Task<IActionResult> Evaluar([FromBody] EvaluacionRetiroDto dto)
        {
            var r = await _retiroService.EvaluarSolicitudCajaAsync(dto);
            if (!r.Success) return BadRequest(new { message = r.Message });
            return Ok(new { message = r.Message });
        }
    }
}
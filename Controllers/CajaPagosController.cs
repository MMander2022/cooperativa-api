using CooperativaApp.DTOS;
using CooperativaApp.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CooperativaApp.Controllers
{
    [ApiController]
    [Route("api/caja/pagos-pendientes")]
    [Authorize]
    public class CajaPagosController : ControllerBase
    {
        private readonly ISolicitudPagoService _solicitudService;

        public CajaPagosController(ISolicitudPagoService solicitudService)
        {
            _solicitudService = solicitudService;
        }

        /// <summary>
        /// 🔍 BANDEJA DE CAJA: Obtiene todas las solicitudes enviadas por socios que están pendientes.
        /// Nivel: Administrativo / Cajero
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPendientes()
        {
            var pendientes = await _solicitudService.ObtenerPendientesAsync();
            return Ok(pendientes);
        }
        [HttpGet("detalle-solicitud/{idSolicitud}")]
        public async Task<IActionResult> GetDetalleSolicitudValidacion(int idSolicitud)
        {
            var detalle = await _solicitudService.GetDetalleSolicitudValidacionAsync(idSolicitud);
            if (detalle == null)
                return NotFound(new { exito = false, mensaje = "Detalle de solicitud no encontrado." });

            return Ok(detalle);
        }
        /// <summary>
        /// 🚀 REGISTRO DE SOCIO: Permite que un socio reporte su voucher/operación.
        /// Nivel: Socio / Admin
        /// </summary>
        [HttpPost("registrar-socio")]
        public async Task<IActionResult> RegistrarSocio([FromForm] RegistrarSolicitudPagoDTO request)
        {
            // Extraemos la identidad del Token
            var perfil = User.FindFirst("Perfil")?.Value ?? "Socio";
            var socioIdStr = User.FindFirst("IdSocio")?.Value;
            int? socioId = !string.IsNullOrEmpty(socioIdStr) ? int.Parse(socioIdStr) : null;

            // 🚀 Enviamos perfil y socioId al servicio para validación inteligente
            var resultado = await _solicitudService.CrearSolicitudSocioAsync(request, perfil, socioId);

            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }
        /// <summary>
        /// ⚖️ PROCESAR SOLICITUD: El cajero aprueba o rechaza el reporte del socio.
        /// Nivel: Administrativo / Cajero
        /// </summary>
            [HttpPost("procesar")]
            public async Task<IActionResult> Procesar([FromBody] ProcesarSolicitudRequest request)
            {
                if (request == null) return BadRequest(new { exito = false, mensaje = "Datos de procesamiento inválidos." });

                // Extraemos el ID del cajero/admin que está operando
                var usuarioIdStr = User.FindFirst("IdUsuario")?.Value;
                int usuarioId = int.Parse(usuarioIdStr ?? "0");

                // Ejecutamos la validación (Si es APROBAR, el servicio debe ejecutar el cobro de cuotas)
                var resultado = await _solicitudService.ProcesarSolicitudAsync(
                    request.IdSolicitud,
                    request.Accion, // "APROBAR" o "RECHAZAR"
                    request.Motivo,
                    usuarioId
                );

            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }
        [HttpGet("precancelacion-simular/{idCredito}")]
        public async Task<IActionResult> SimularPrecancelacion(int idCredito)
        {
            var simulacion = await _solicitudService.ObtenerSimulacionPrecancelacionAsync(idCredito);

            if (simulacion == null)
                return NotFound(new { exito = false, mensaje = "Crédito no encontrado o sin cuotas pendientes." });

            return Ok(simulacion);
        }


    }
}
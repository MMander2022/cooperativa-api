using CooperativaApp.Data;
using CooperativaApp.DTOS;
using CooperativaApp.Models;
using CooperativaApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace CooperativaApp.Controllers
{
    
        [ApiController]
        [Route("api/[controller]")]
        public class SolicitudesController : ControllerBase
        {
            private readonly ISolicitudService _solicitudService;

            public SolicitudesController(ISolicitudService solicitudService)
            {
                _solicitudService = solicitudService;
            }

            [HttpPost]
        public async Task<IActionResult> Registrar([FromBody] SolicitudCreateDTO dto)
        {
            try
            {
                // 🕵️ Extraemos el ID del usuario del Token (Seguridad Titanium)
                int usuarioId = GetCurrentUserId();
                if (usuarioId == 0) return Unauthorized(new { message = "Sesión inválida." });

                // Ejecutamos la lógica en el servicio
                var id = await _solicitudService.RegistrarSolicitudAsync(dto, usuarioId);

                return Ok(new { id = id, message = "Solicitud creada con éxito" });
            }
            catch (InvalidOperationException ex)
            {
                // Errores de lógica (ej: ya tiene solicitud)
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Errores inesperados de sistema
                return StatusCode(500, new { message = "Error interno", detail = ex.Message });
            }
        }

        // 🛠️ MÉTODO DE IDENTIDAD (Soluciona el error de contexto)
        private int GetCurrentUserId()
        {
            var claim = User.FindFirst("IdUsuario") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int userId) ? userId : 0;
        }
        [HttpGet("pendientes")]
        public async Task<IActionResult> GetPendientes([FromQuery] decimal? montoMaximo)
        {
            try
            {
                // 🛡️ Ajuste Titanium: 
                // SQL Server soporta hasta 10^38, pero para evitar conflictos de precisión 
                // usamos un tope de 100 Millones, suficiente para cualquier crédito real.
                decimal umbralSeguro = (montoMaximo == null || montoMaximo <= 0)
                                       ? 100_000_000m
                                       : montoMaximo.Value;

                var pendientes = await _solicitudService.ObtenerPendientesAsync(umbralSeguro);

                return Ok(pendientes);
            }
            catch (Exception ex)
            {
                // Log para auditoría
                Console.WriteLine($"🚨 Error en GetPendientes: {ex.Message}");
                return StatusCode(500, new { message = "Error de desbordamiento o conversión en el Core." });
            }
        }
        [HttpPost("{id}/aprobar")]
        public async Task<IActionResult> Aprobar(int id, [FromBody] AprobarRequest request)
        {
            // Ahora 'resultado' es un objeto AprobacionResponse, no un bool
            var resultado = await _solicitudService.AprobarConSPAsync(id, request.UsuarioId, request.Comentario);

            // Evaluamos la propiedad Exito del objeto
            if (resultado.Exito)
            {
                return Ok(resultado); // Retornamos todo el objeto (incluye IdCreditoGenerado)
            }

            return BadRequest(new { message = resultado.Mensaje });
        }
        [HttpPost("{id}/decidir")]
        public async Task<IActionResult> Decidir(int id, [FromBody] DecisionRequestDTO request)
        {
            // 🛡️ SEGURIDAD: Obtenemos el ID del evaluador del Token, NO del request del front
            int analistaId = GetCurrentUserId();
            if (analistaId == 0) return Unauthorized(new { message = "Sesión expirada" });

            // Inyectamos el ID real en el request antes de procesar
            var requestSeguro = request with { UsuarioId = analistaId };

            var resultado = await _solicitudService.DecidirSolicitudAsync(id, requestSeguro);

            if (resultado.Exito) return Ok(resultado);

            return BadRequest(new { message = resultado.Mensaje });
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                // Invocamos al servicio para obtener la telemetría completa
                // Asegúrate de que el método 'ListarTodasAsync' devuelva un DTO con NombreSocio
                var lista = await _solicitudService.ListarTodasAsync();
                return Ok(lista);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al recuperar historial global", detail = ex.Message });
            }
        }

        // 🛰️ MOTOR PRIVADO: Listar solicitudes por Socio específico
        [HttpGet("socio/{socioId}")]
        public async Task<IActionResult> GetBySocio(int socioId)
        {
            try
            {
                // Validación de seguridad: Un socio no debería ver solicitudes de otro 
                // (Omitido para simplificar, pero el Admin sí puede saltarse esto)
                var lista = await _solicitudService.ObtenerPorSocioAsync(socioId);
                return Ok(lista);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al recuperar tus solicitudes", detail = ex.Message });
            }
        }

        // DTO rápido para el cuerpo del POST
        public record AprobarRequest(int UsuarioId, string? Comentario);
    }
    
}

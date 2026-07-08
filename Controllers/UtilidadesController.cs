using CooperativaApp.Data;
using CooperativaApp.DTOs;
using CooperativaApp.Models;
using CooperativaApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CooperativaApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UtilidadesController : ControllerBase
    {
        private readonly IUtilidadService _utilidadService;
        private readonly CooperativaContext _context;

        public UtilidadesController(IUtilidadService utilidadService, CooperativaContext context)
        {
            _utilidadService = utilidadService ?? throw new ArgumentNullException(nameof(utilidadService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [Authorize(Roles = "ADMIN,ADMINISTRADOR,GERENTE")]
        [HttpPost("procesar-mensual")]
        public async Task<IActionResult> ProcesarMensual([FromBody] ProcesarUtilidadDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // 1. Extraer ID del Usuario Administrador desde el Token JWT de forma segura
                var stringUsuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                int idUsuarioAdmin = 0;
                if (!string.IsNullOrEmpty(stringUsuarioId))
                {
                    idUsuarioAdmin = Convert.ToInt32(stringUsuarioId);
                }

                // 2. Validar que la ventana de configuración del periodo esté HABILITADA
                bool esPeriodoValido = await _utilidadService.ValidarEstadoPeriodoConfigAsync(dto.IdPeriodoConfig);
                if (!esPeriodoValido)
                    return BadRequest(new { Message = "El periodo seleccionado no está habilitado o ya fue cerrado por gerencia." });

                // 3. Validar que el mes y año no hayan sido procesados previamente (Evita sobre-escrituras)
                bool yaFueProcesado = await _utilidadService.VerificarPeriodoProcesadoAsync(dto.Mes, dto.Anio);
                if (yaFueProcesado)
                    return BadRequest(new { Message = $"Las utilidades contables para el periodo {dto.Mes}/{dto.Anio} ya se encuentran consolidadas." });

                // 4. Invocar la ejecución atómica del Stored Procedure
                await _utilidadService.EjecutarAlgoritmoProrrateoAsync(dto.IdPeriodoConfig, dto.Mes, dto.Anio, idUsuarioAdmin);

                return Ok(new
                {
                    Message = "¡Proceso Completado!",
                    Detalle = $"Las utilidades del periodo {dto.Mes}/{dto.Anio} fueron calculadas y prorrateadas exitosamente a todo el núcleo de socios."
                });
            }
            catch (Exception ex)
            {
                // 🎯 CORRECCIÓN QUIRÚRGICA: Cambiado '||' por '??' para evaluar nulos en strings de excepciones
                return StatusCode(500, new
                {
                    Message = "Error en el procesamiento contable.",
                    Detalle = ex.InnerException?.Message ?? ex.Message
                });
            }
        }
        [Authorize(Roles = "SOCIO,ESTANDAR")]
        [HttpGet("mi-saldo-disponible")]
        public async Task<IActionResult> GetMiSaldoDisponible()
        {
            try
            {
                // 1. Extraer e identificar al socio de forma unificada
                var stringSocioId = User.FindFirst("IdSocio")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int idSocioLogueado = Convert.ToInt32(stringSocioId ?? "0");

                if (idSocioLogueado == 0)
                    return BadRequest(new { Message = "No se pudo identificar la sesión del socio activo." });

                // 2. Buscar si hay un periodo activo para retiros
                var periodo = await _context.PeriodosRetiroUtilidad
                    .FirstOrDefaultAsync(p => p.Estado == "PROCESADO" || p.Estado == "HABILITADO");

                if (periodo == null)
                    return Ok(new { MontoDisponible = 0, PeriodoConfig = (object)null! });

                // 3. Sumar el saldo disponible remanente (Corregido: usando idSocioLogueado)
                var disponible = await _context.UtilidadesProcesadas
                    .Where(u => u.IdSocio == idSocioLogueado && u.IdPeriodoConfig == periodo.IdPeriodoConfig)
                    .SumAsync(u => u.MontoDisponible);

                return Ok(new
                {
                    MontoDisponible = disponible,
                    PeriodoConfig = new
                    {
                        idPeriodoConfig = periodo.IdPeriodoConfig,
                        nombrePeriodo = periodo.NombrePeriodo
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error al recuperar saldos.", Detalle = ex.Message });
            }
        }

        [Authorize(Roles = "SOCIO")]
        [HttpPost("solicitar-pago")]
        public async Task<IActionResult> SolicitarPago([FromBody] SolicitudRetiroDTO dto)
        {
            try
            {
                // Corregido: unificando el nombre de la variable para el ID del socio
                var stringSocioId = User.FindFirst("IdSocio")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int idSocio = Convert.ToInt32(stringSocioId ?? "0");

                if (idSocio == 0)
                    return BadRequest(new { Message = "Socio no identificado." });

                var disponible = await _context.UtilidadesProcesadas
                    .Where(u => u.IdSocio == idSocio && u.IdPeriodoConfig == dto.IdPeriodoConfig)
                    .SumAsync(u => u.MontoDisponible);

                if (dto.MontoSolicitado > disponible)
                    return BadRequest(new { Message = "El monto solicitado excede sus utilidades disponibles." });

                var solicitud = new SolicitudUtilidad
                {
                    IdSocio = idSocio,
                    IdPeriodoConfig = dto.IdPeriodoConfig,
                    MontoSolicitado = dto.MontoSolicitado,
                    TipoRetiro = dto.MontoSolicitado == disponible ? "TOTAL" : "PARCIAL",
                    Estado = "PENDIENTE",
                    FechaSolicitud = DateTime.Now
                };

                _context.SolicitudesUtilidad.Add(solicitud);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Solicitud enviada a Caja correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error al registrar la solicitud.", Detalle = ex.Message });
            }
        }
    }
}
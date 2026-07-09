using CooperativaApp.Data;
using CooperativaApp.DTOs;
using CooperativaApp.Models;
using CooperativaApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Linq;
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

        // ── 🛰️ EL AJUSTE GALÁCTICO: SE ADICIONA EL GET FALTANTE PARA REPARAR EL 404 ──
        [Authorize]
        [HttpGet("periodos-configuracion")]
        public async Task<IActionResult> ListarPeriodos()
        {
            try
            {
                var periodos = await _context.PeriodosRetiroUtilidad
                    .OrderByDescending(p => p.IdPeriodoConfig)
                    .Select(p => new
                    {
                        idPeriodoConfig = p.IdPeriodoConfig,
                        nombrePeriodo = p.NombrePeriodo,
                        fechaInicioCalculo = p.FechaInicioCalculo,
                        fechaFinCalculo = p.FechaFinCalculo,
                        fechaAperturaRetiro = p.FechaAperturaRetiro,
                        fechaCierreRetiro = p.FechaCierreRetiro,
                        estado = p.Estado
                    })
                    .ToListAsync();

                return Ok(periodos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error al recuperar listado de base de datos.", Detalle = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("procesar-mensual")]
        public async Task<IActionResult> ProcesarMensual([FromBody] ProcesarUtilidadDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var stringUsuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                int idUsuarioAdmin = 0;
                if (!string.IsNullOrEmpty(stringUsuarioId))
                {
                    idUsuarioAdmin = Convert.ToInt32(stringUsuarioId);
                }

                bool esPeriodoValido = await _utilidadService.ValidarEstadoPeriodoConfigAsync(dto.IdPeriodoConfig);
                if (!esPeriodoValido)
                    return BadRequest(new { Message = "El periodo seleccionado no está habilitado o ya fue cerrado por gerencia." });

                bool yaFueProcesado = await _utilidadService.VerificarPeriodoProcesadoAsync(dto.Mes, dto.Anio);
                if (yaFueProcesado)
                    return BadRequest(new { Message = $"Las utilidades contables para el periodo {dto.Mes}/{dto.Anio} ya se encuentran consolidadas." });

                await _utilidadService.EjecutarAlgoritmoProrrateoAsync(dto.IdPeriodoConfig, dto.Mes, dto.Anio, idUsuarioAdmin);

                return Ok(new
                {
                    Message = "¡Proceso Completado!",
                    Detalle = $"Las utilidades del periodo {dto.Mes}/{dto.Anio} fueron calculadas y prorrateadas exitosamente a todo el núcleo de socios."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "Error en el procesamiento contable.",
                    Detalle = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [Authorize] // Inyectamos restricción de roles coherente aquí también si aplica
        [HttpPost("configurar-periodo")]
        public async Task<IActionResult> ConfigurarPeriodo([FromBody] PeriodosRetiroUtilidad modelo)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                modelo.Estado = "CONFIGURADO";

                await _utilidadService.RegistrarPeriodoConfiguracionAsync(modelo);

                return Ok(new
                {
                    Message = "Estructura guardada exitosamente.",
                    idPeriodoConfig = modelo.IdPeriodoConfig
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error al persistir la ventana fiscal.", Detalle = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("mi-saldo-disponible")]
        public async Task<IActionResult> GetMiSaldoDisponible()
        {
            try
            {
                var stringSocioId = User.FindFirst("IdSocio")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int idSocioLogueado = Convert.ToInt32(stringSocioId ?? "0");

                if (idSocioLogueado == 0)
                    return BadRequest(new { Message = "No se pudo identificar la sesión del socio activo." });

                var periodo = await _context.PeriodosRetiroUtilidad
                    .FirstOrDefaultAsync(p => p.Estado == "PROCESADO" || p.Estado == "HABILITADO");

                if (periodo == null)
                    return Ok(new { MontoDisponible = 0, PeriodoConfig = (object)null! });

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

        [Authorize]
        [HttpPost("solicitar-pago")]
        public async Task<IActionResult> SolicitarPago([FromBody] SolicitudRetiroDTO dto)
        {
            try
            {
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
        [Authorize()]
        [HttpGet("simular-prorrateo")]
        public async Task<IActionResult> SimularProrrateo([FromQuery] int idPeriodoConfig, [FromQuery] int mes, [FromQuery] int anio)
        {
            try
            {
                // 1. Validar estado del periodo seleccionado
                bool esPeriodoValido = await _utilidadService.ValidarEstadoPeriodoConfigAsync(idPeriodoConfig);
                if (!esPeriodoValido)
                    return BadRequest(new { Message = "El periodo seleccionado no está habilitado o ya fue cerrado." });

                // 2. Extraer el DataTable del Stored Procedure en modo simulación
                DataTable table = await _utilidadService.SimularProrrateoMensualAsync(idPeriodoConfig, mes, anio);

                if (table.Rows.Count == 0)
                    return Ok(new { detalles = new List<object>() });

                // 3. Mapeamos la lista completa con la traza multimes para el Front-End
                var listadoPlano = table.AsEnumerable().Select(row => new
                {
                    periodoNombre = row["PeriodoNombre"].ToString(),
                    mesEvaluado = row["MesEvaluado"].ToString(),
                    anioFiscal = Convert.ToInt32(row["AnioFiscal"]),
                    interesMensualBruto = Convert.ToDecimal(row["InteresMensualBruto"]),
                    gastoMensual = Convert.ToDecimal(row["GastoMensual"]),
                    totalAportesConsolidado = Convert.ToDecimal(row["TotalAportesConsolidado"]),
                    totalUtilidadConsolidada = Convert.ToDecimal(row["TotalUtilidadConsolidada"]),
                    idSocio = Convert.ToInt32(row["IdSocio"]),
                    codigoSocio = row["CodigoSocio"].ToString(),
                    nombreCompleto = row["NombreCompleto"].ToString(),
                    aporteAcumulado = Convert.ToDecimal(row["AporteAcumulado"]),
                    aporteDelMes = Convert.ToDecimal(row["AporteDelMes"]),
                    utilidadGenerada = Convert.ToDecimal(row["UtilidadGenerada"]),
                    aporteAcumuladoFinal = Convert.ToDecimal(row["AporteAcumuladoFinal"])
                }).ToList();

                return Ok(listadoPlano);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Validación del Sistema", Detalle = ex.Message });
            }
        }

        [Authorize()] // Ajusta los roles según tu sistema
        [HttpGet("consultar-historial")]
        public async Task<IActionResult> ConsultarHistorial([FromQuery] int idPeriodoConfig, [FromQuery] int mes, [FromQuery] int anio)
        {
            // 1. Validaciones preventivas de consistencia de parámetros
            if (idPeriodoConfig <= 0)
            {
                return BadRequest(new { Message = "El identificador del periodo configurado no es válido." });
            }

            if (mes < 1 || mes > 12)
            {
                return BadRequest(new { Message = "El mes proporcionado debe estar en el rango de 1 a 12." });
            }

            if (anio < 2000)
            {
                return BadRequest(new { Message = "El año proporcionado no corresponde a un periodo fiscal válido." });
            }

            try
            {
                // 2. Consumimos el servicio optimizado en memoria que no bloquea la BD
                var historial = await _utilidadService.ObtenerHistorialProcesadoAsync(idPeriodoConfig, mes, anio);

                // 3. Si no hay registros consolidados, retornamos un estado neutro limpio
                if (historial == null)
                {
                    return Ok(new object[] { });
                }

                return Ok(historial);
            }
            catch (Exception ex)
            {
                // NOTA: Aquí puedes meter tu logger interno (ej. Serilog, NLog o ILogger) 
                // para registrar la traza completa: _logger.LogError(ex, "Error en ConsultarHistorial");

                return StatusCode(500, new
                {
                    Message = "Ocurrió un error inesperado en el servidor al recuperar el historial contable.",
                    Detalle = ex.Message
                });
            }
        }
    }
}
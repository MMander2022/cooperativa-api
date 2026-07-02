using CooperativaApp.Data;
using CooperativaApp.DTOs; // Asegúrate de que tus DTOs estén aquí
using CooperativaApp.DTOS;
using CooperativaApp.Interfaces;
using CooperativaApp.Models;
using CooperativaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CooperativaApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CreditosController : ControllerBase
    {
        private readonly CooperativaContext _context;
        private readonly ICreditoService _creditoService; // 🔹 CAMBIO: Usar Interfaz
        

        // 🔹 Inyectamos ICreditoService para que coincida con Program.cs
        public CreditosController(CooperativaContext context, ICreditoService creditoService)
        {
            _context = context;
            _creditoService = creditoService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CreditoListadoDTO>>> Get()
        {
            try
            {
                // 📡 Escaneo de base de datos con Include para el Join
                var lista = await _context.Creditos
                    .Include(c => c.Socio)
                    .OrderByDescending(c => c.IdCredito)
                    .Select(c => new CreditoListadoDTO
                    {
                        IdCredito = c.IdCredito,
                        // Si la relación existe, trae el nombre; si no, marca error controlado
                        NombreSocio = c.Socio != null ? c.Socio.Nombres + " " + c.Socio.Apellidos : "SOCIO SIN IDENTIFICAR",
                        Dni = c.Socio != null ? c.Socio.DNI : "",
                        Monto = c.Monto,
                        TasaInteres = c.TasaInteres,
                        PlazoMeses = c.PlazoMeses,
                        FechaAprobacion = c.FechaAprobacion,
                        Estado = c.Estado,
                        TipoCalculo = c.TipoCalculo,
                        MontoDesembolsado=c.MontoDesembolsado
                    })
                    .ToListAsync();

                return Ok(lista);
            }
            catch (Exception ex)
            {
                // 🛡️ Ahora _logger ya existe y grabará el fallo
                //_logger.LogError(ex, "🚨 Falla en el motor de listado de cartera.");
                return StatusCode(500, "Error interno en el Core.");
            }
        }
        [HttpPost]
        public async Task<ActionResult> Crear(Credito credito)
        {
            credito.FechaSolicitud = DateTime.Now;
            credito.Estado = "SOLICITADO";
            _context.Creditos.Add(credito);
            await _context.SaveChangesAsync();
            return Ok(credito);
        }

        // 🔹 Delegamos al servicio (reutilizamos tu lógica financiera)
        //[HttpPost("{idCredito}/desembolsar")]
        //public async Task<IActionResult> Desembolsar(int idCredito, [FromBody] DesembolsoRequest request)
        //{
        //    try
        //    {
        //        var resultado = await _creditoService.DesembolsarCreditoAsync(idCredito, request.UsuarioId);
        //        return Ok(new { message = "Éxito", transaccion = resultado });
        //    }
        //    catch (Exception ex)
        //    {
        //        // Ahora ex.Message traerá el mensaje que extrajimos en el Service
        //        return StatusCode(500, new
        //        {
        //            error = "Error de Persistencia",
        //            detalle = ex.Message
        //        });
        //    }
        //}
        [HttpGet("socio/{socioId}")]
        public async Task<IActionResult> GetBySocio(int socioId)
        {
            try
            {
                var data = await _creditoService.ObtenerCreditosPorSocioAsync(socioId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error al recuperar tus créditos",
                    detail = ex.Message
                });

            }
        }

        // 🔹 ESTA ES LA RUTA QUE BUSCA SU FRONT: Creditos/${creditoId}/plan-pagos
        [HttpGet("{id}/plan-pagos")]
        public async Task<IActionResult> GetPlanPagos(int id)
        {
            var data = await _creditoService.ObtenerPlanPagosAsync(id);
            if (data == null) return NotFound();
            return Ok(data);
        }
        [HttpPost("desembolsar")]
        public async Task<IActionResult> Desembolsar([FromBody] DesembolsoRequest request)
        {
            // Validamos que el monto sea positivo (Regla Senior)
            if (request.Monto <= 0) return BadRequest("El monto a desembolsar debe ser mayor a cero.");

            var resultado = await _creditoService.RegistrarDesembolsoAsync(request);

            if (resultado.Exito)
                return Ok(resultado);

            return BadRequest(new { message = resultado.Mensaje });
        }
        //public class DesembolsoRequest { public int UsuarioId { get; set; } }

        [HttpGet("perfil")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<CreditoSocioDTO>>> GetCreditosPorPerfil()
        {
            // 🕵️ Extraemos datos del Token (Inyectados por el AuthMiddleware)
            var perfil = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Socio";
            var socioIdStr = User.FindFirst("IdSocio")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? socioId = !string.IsNullOrEmpty(socioIdStr) ? int.Parse(socioIdStr) : null;
            var idUsuarioToken = User.FindFirst("IdUsuario")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int usuarioId = int.Parse(idUsuarioToken);
            
            // Delegamos al servicio diamante
            var creditos = await _creditoService.ObtenerCreditosPorPerfilAsync(usuarioId, perfil, socioId);

            return Ok(creditos);
        }

        [HttpGet("{id}/plan-detallado")]
        public async Task<IActionResult> GetPlanDetallado(int id)
        {
            // El id es el IdCredito
            var plan = await _creditoService.GetPlanPagosConAuditoriaAsync(id);

            if (plan == null || !plan.Any())
                return NotFound(new { mensaje = "No se encontró plan de pagos para este crédito." });

            return Ok(plan);
        }
        [HttpGet("plan-analitico/{id}")]
        public async Task<IActionResult> GetPlanAnalitico(int id)
        {
            var data = await _creditoService.GetPlanPagosAnaliticoAsync(id);
            return Ok(data);
        }
    }
}
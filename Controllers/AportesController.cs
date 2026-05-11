using CooperativaApp.Data;
using CooperativaApp.Interfaces;
using CooperativaApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CooperativaApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AportesController : ControllerBase
    {
        private readonly IAporteService _aporteService;
        private readonly CooperativaContext _context;

        public AportesController(IAporteService aporteService, CooperativaContext context)
        {
            _aporteService = aporteService;
            _context = context;
        }

        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar(AporteSocio aporte)
        {
            var idSocioToken = User.FindFirst("IdSocio")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var idUsuarioToken = User.FindFirst("IdUsuario")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!User.IsInRole("Admin") && !string.IsNullOrEmpty(idSocioToken))
            {
                aporte.IdSocio = int.Parse(idSocioToken);
            }

            if (!string.IsNullOrEmpty(idUsuarioToken))
            {
                aporte.IdUsuarioRegistro = int.Parse(idUsuarioToken);
            }

            var result = await _aporteService.RegistrarAporteAsync(aporte);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }


        [HttpGet("configuracion-actual")]

        public async Task<IActionResult> GetConfig()

        {

            try

            {

                // Llamamos al método que busca el registro con Estado = 1 y FechaFin = null

                var config = await _aporteService.GetConfiguracionVigenteAsync();



                if (config == null) return NotFound("No hay configuración de acciones vigente.");



                return Ok(new

                {

                    idConfig = config.IdConfig,

                    valorAccion = config.ValorAccion // O ValorAccion, según nombraste el campo

                });

            }

            catch (Exception ex)

            {

                return BadRequest(ex.Message);

            }

        }
        [HttpGet("mis-aportes")]
        public async Task<IActionResult> GetMisAportes()
        {
            try
            {
                var idSocioClaim = User.FindFirst("IdSocio")?.Value;
                var perfilClaim = User.FindFirst("Perfil")?.Value?.ToUpper() ?? "";
                bool isAdmin = perfilClaim == "ADMIN" || perfilClaim == "ADMINISTRADOR" || perfilClaim == "GERENTE";

                // 1. Iniciamos Query con Includes tácticos
                var query = _context.AportesSocios
                    .Include(a => a.Socio)
                    .Include(a => a.MedioPago)
                    .AsNoTracking()
                    .AsQueryable();

                // 2. FILTRO DE SEGURIDAD NÚCLEO FAMILIAR (Mega Diamante)
                if (!isAdmin)
                {
                    int idSocioSesion = 0;
                    if (!string.IsNullOrEmpty(idSocioClaim)) int.TryParse(idSocioClaim, out idSocioSesion);

                    // 🛰️ RADAR DE FAMILIARIDAD: Buscamos los IDs de los familiares activos
                    var idsFamilia = await _context.Familiaridad
                        .Where(f => f.IdSocioTitular == idSocioSesion && f.Activo)
                        .Select(f => f.IdSocioFamiliar)
                        .ToListAsync();

                    // Incluimos al titular en el radio de búsqueda
                    idsFamilia.Add(idSocioSesion);

                    // Filtramos la query para que solo devuelva aportes del núcleo familiar
                    query = query.Where(a => idsFamilia.Contains(a.IdSocio));
                }

                // 3. ORDENAMIENTO (Socio -> Año -> Mes Creciente para facilitar auditoría familiar)
                if (isAdmin || (!isAdmin && query.Select(x => x.IdSocio).Distinct().Count() > 1))
                {
                    // Si es admin o un titular con familia, ordenamos por nombre para agruparlos visualmente
                    query = query
                        .OrderBy(a => a.Socio.Nombres)
                        .ThenBy(a => a.Socio.Apellidos)
                        .ThenBy(a => a.AnioAportado)
                        .ThenBy(a => a.MesAportado);
                }
                else
                {
                    // Socio individual: Orden cronológico descendente
                    query = query.OrderByDescending(a => a.AnioAportado).ThenByDescending(a => a.MesAportado);
                }

                // 4. Proyección de Datos Blindada
                var aportes = await query
                    .Where(a => a.EstadoPago != 'E') // Excluir eliminados
                    .Select(a => new {
                        idAporte = a.IdAporte,
                        idSocio = a.IdSocio,
                        nombreSocio = $"{a.Socio.Nombres} {a.Socio.Apellidos}".ToUpper(),
                        montoPagado = decimal.Round(a.MontoPagado, 2),
                        periodo = $"{a.MesAportado:00}-{a.AnioAportado}",
                        mesAportado = a.MesAportado, // Necesario para el front
                        anioAportado = a.AnioAportado, // Necesario para el front
                        medioPago = a.MedioPago != null ? a.MedioPago.Nombre : "NO ESPECIFICADO",
                        idMedioPago = a.IdMedioPago,
                        estadoPago = a.EstadoPago.ToString(),
                        fechaPago = a.FechaPago
                    })
                    .ToListAsync();

                return Ok(aportes);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error en búnker de aportes familiar: {ex.Message}" });
            }
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarAporte(int id, [FromBody] AporteSocio aporteData)
        {
            var aporte = await _context.AportesSocios.FindAsync(id);
            if (aporte == null) return NotFound(new { message = "Aporte no encontrado." });

            // 🛡️ REGLA TITANIUM: No editar si ya está aprobado
            if (aporte.EstadoPago == 'A')
                return BadRequest(new { message = "No se puede editar un aporte ya aprobado por tesorería." });

            aporte.MontoPagado = aporteData.MontoPagado;
            aporte.CantidadAcciones = aporteData.CantidadAcciones;
            aporte.MesAportado = aporteData.MesAportado;
            aporte.AnioAportado = aporteData.AnioAportado;
            aporte.UrlEvidencia = aporteData.UrlEvidencia;
            aporte.IdMedioPago = aporteData.IdMedioPago;

            // Si estaba rechazado, vuelve a pendiente para nueva revisión
            if (aporte.EstadoPago == 'R') aporte.EstadoPago = 'P';

            await _context.SaveChangesAsync();
            return Ok(new { message = "Aporte actualizado correctamente." });
        }

        [HttpDelete("eliminar/{id}")]
        public async Task<IActionResult> EliminarAporte(int id)
        {
            var aporte = await _context.AportesSocios.FindAsync(id);
            if (aporte == null) return NotFound(new { message = "Aporte no encontrado." });

            // 🛡️ REGLA TITANIUM: Solo se elimina si no ha sido aprobado
            if (aporte.EstadoPago == 'A')
                return BadRequest(new { message = "Operación denegada: Los aportes aprobados no pueden eliminarse." });

            aporte.EstadoPago = 'E'; // Eliminado lógico
            await _context.SaveChangesAsync();

            return Ok(new { message = "Aporte cancelado exitosamente." });
        }

        [HttpGet("pendientes")]

        public async Task<IActionResult> GetPendientes()

        {

            Console.WriteLine("--------------------------------------------------");

            Console.WriteLine("🔍 INSPECCIÓN DE RADAR: GetPendientes invocado");



            try

            {

                // 1. Verificamos el Contexto

                if (_context == null) throw new Exception("❌ El Contexto de Base de Datos es NULO");

             
                // 2. Ejecutamos la consulta

                var lista = await _context.AportesSocios

                    .Include(a => a.Socio)
                    .Include(a => a.MedioPago)
                    .Include(a => a.ConfigAporte)

                    .Where(a => a.EstadoPago == 'P')

                    .ToListAsync();



                Console.WriteLine($"📊 REGISTROS ENCONTRADOS: {lista.Count}");



                // 3. Pintamos el primer registro si existe para detectar NULOS

                if (lista.Any())

                {

                    var primero = lista.First();

                    Console.WriteLine($"🆔 Primer Aporte ID: {primero.IdAporte}");

                    Console.WriteLine($"👤 Socio Relacionado: {(primero.Socio != null ? "OK" : "NULO 🚩")}");

                    Console.WriteLine($"⚙️ Config Relacionada: {(primero.ConfigAporte != null ? "OK" : "NULO 🚩")}");

                }



                var resultado = lista.Select(a => new {

                    IdAporte = a.IdAporte,

                    SocioNombre = a.Socio != null ? $"{a.Socio.Nombres} {a.Socio.Apellidos}" : "Socio Huérfano",

                    a.MontoPagado,

                    a.EstadoPago,
                    periodo = $"{a.MesAportado:00}-{a.AnioAportado}",
                    mesAportado = a.MesAportado, // Necesario para el front
                    anioAportado = a.AnioAportado, // Necesario para el front
                    medioPago = a.MedioPago != null ? a.MedioPago.Nombre : "NO ESPECIFICADO",
                    idMedioPago = a.IdMedioPago,


                }).ToList();



                return Ok(resultado);

            }

            catch (Exception ex)

            {

                Console.WriteLine($"❌ ERROR CRÍTICO: {ex.Message}");

                Console.WriteLine($"📂 STACKTRACE: {ex.StackTrace}");

                return StatusCode(500, ex.Message);

            }

        }

        [HttpPost("aprobar/{id}")]

        public async Task<IActionResult> Aprobar(int id)

        {

            using var transaction = await _context.Database.BeginTransactionAsync();



            try

            {

                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value

                   ?? User.FindFirst("IdUsuario")?.Value

                   ?? "0";



                int idUsuarioProcesa = int.Parse(userIdClaim);



                if (idUsuarioProcesa == 0)

                {

                    // 🚩 Si sigue siendo 0, pintamos en consola para ver qué trae el Token realmente

                    foreach (var claim in User.Claims)

                    {

                        Console.WriteLine($"🛰️ Claim detectado: {claim.Type} = {claim.Value}");

                    }

                    return BadRequest("No se pudo identificar al usuario que procesa (ID 0).");

                }

                // 1. Localizar el Aporte y sus datos

                var aporte = await _context.AportesSocios

                    .Include(a => a.ConfigAporte)

                    .FirstOrDefaultAsync(a => a.IdAporte == id);



                if (aporte == null) return NotFound("Aporte no detectado en el radar.");



                // 2. Obtener el IdConcepto dinámicamente (Evitamos Hardcode)

                var conceptoAporte = await _context.ConceptosOperacion

                    .FirstOrDefaultAsync(c => c.Nombre == "APORTE DE SOCIO" && c.TipoMovimiento == "I");



                if (conceptoAporte == null)

                    return BadRequest("Error: El concepto 'APORTE DE SOCIO' no existe en la tabla de Conceptos.");



                // 3. Actualizar el Aporte

                aporte.EstadoPago = 'A';

                aporte.FechaValidacion = DateTime.Now;



                // 4. Generar el Movimiento de Caja 🚀

                var movimiento = new MovimientoCaja

                {

                    IdConcepto = conceptoAporte.IdConcepto, // Obtenido de la tabla Conceptos

                    IdCredito = aporte.IdAporte,            // Aquí vinculamos el Aporte

                    Monto = aporte.MontoPagado,

                    Fecha = DateTime.Now,

                    IdUsuario = int.Parse(User.FindFirst("IdUsuario")?.Value ?? "0"),

                    Estado = "PROCESADO",
                    IdMedioPago=aporte.IdMedioPago,


                    IdCaja = 1 // Debería venir del turno de caja activo del usuario

                    // Nota: El detalle "Aporte Mensual..." se puede usar para el Asiento Contable

                };



                _context.MovimientosCaja.Add(movimiento);



                // 5. Persistencia y Cierre de Misión

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();



                return Ok(new { message = "Conciliación exitosa. Movimiento registrado." });

            }

            catch (Exception ex)

            {

                await transaction.RollbackAsync();

                return BadRequest($"Falla en el motor de aprobación: {ex.Message}");

            }

        }



        [HttpPost("rechazar/{id}")]

        public async Task<IActionResult> Rechazar(int id, [FromBody] string motivo)

        {

            var aporte = await _context.AportesSocios.FindAsync(id);

            if (aporte == null) return NotFound();



            try

            {

                aporte.EstadoPago = 'R'; // 'R' de Rechazado/Observado

                aporte.ComentarioCaja = motivo; // Asegúrate de tener este campo en tu BD



                await _context.SaveChangesAsync();

                return Ok(new { message = "Aporte rechazado con observación." });

            }

            catch (Exception ex)

            {

                return BadRequest(ex.Message);

            }

        }


        // Se mantienen intactos los métodos Aprobar, Rechazar, GetConfig y GetPendientes...
    }
}
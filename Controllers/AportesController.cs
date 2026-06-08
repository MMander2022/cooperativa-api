using CooperativaApp.Data;
using CooperativaApp.DTOS;
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
        private readonly BlobStorageService _blobService;
        public AportesController(IAporteService aporteService, CooperativaContext context, BlobStorageService blobService)
        {
            _aporteService = aporteService;
            _context = context;
            _blobService = blobService;
        }
        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromForm] AporteRequestDto dto)
        {
            var idSocioToken = User.FindFirst("IdSocio")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var idUsuarioToken = User.FindFirst("IdUsuario")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool esAdmin = User.IsInRole("Admin") || User.IsInRole("Administrador") || User.IsInRole("Gerente");

            // Mapeamos los datos del formulario a la entidad de negocio
            var aporte = new AporteSocio
            {
                IdSocio = dto.IdSocio,
                CantidadAcciones = dto.CantidadAcciones,
                IdMedioPago = dto.IdMedioPago,
                MesAportado = dto.MesAportado,
                AnioAportado = dto.AnioAportado
            };

            if (!string.IsNullOrEmpty(idUsuarioToken))
                aporte.IdUsuarioRegistro = int.Parse(idUsuarioToken);

            if (!esAdmin)
            {
                // Si el socio NO seleccionó a nadie o se seleccionó a sí mismo — usar token
                if (aporte.IdSocio == 0 || aporte.IdSocio == int.Parse(idSocioToken ?? "0"))
                {
                    aporte.IdSocio = int.Parse(idSocioToken ?? "0");
                }
                else
                {
                    // Verificar que el IdSocio seleccionado pertenece al núcleo familiar
                    var socioLogueado = int.Parse(idSocioToken ?? "0");
                    var perteneceAlNucleo = await _context.Familiaridad
                        .AnyAsync(n =>
                            (n.IdSocioTitular == socioLogueado && n.IdSocioFamiliar == aporte.IdSocio) ||
                            (n.IdSocioFamiliar == socioLogueado && n.IdSocioTitular == aporte.IdSocio)
                        );

                    if (!perteneceAlNucleo)
                        return BadRequest(new { message = "Seguridad: El socio seleccionado no pertenece a su núcleo familiar." });
                }
            }

            // 🚀 SUBIDA AL CONTENEDOR DE AZURE BLOB STORAGE
            if (dto.ArchivoVoucher != null && dto.ArchivoVoucher.Length > 0)
            {
                try
                {
                    // Se sube el archivo al contenedor "vouchers" en Azure
                    string urlAzure = await _blobService.UploadVoucherAsync(dto.ArchivoVoucher);

                    // Se le asigna la URL pública generada a la entidad antes de ir al servicio
                    aporte.UrlEvidencia = urlAzure;
                }
                catch (System.Exception ex)
                {
                    return StatusCode(500, new { message = "Error de infraestructura al guardar la imagen en Azure.", detalles = ex.Message });
                }
            }

            // Se envía la entidad con la UrlEvidencia ya poblada al servicio de persistencia
            var result = await _aporteService.RegistrarAporteAsync(aporte);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message, url = aporte.UrlEvidencia });
        }

        [HttpGet("configuracion-actual")]
        public async Task<IActionResult> GetConfig()
        {
            try
            {
                var config = await _context.ConfigAportes
                    .AsNoTracking()
                    .OrderByDescending(x => x.FechaInicio)
                    .FirstOrDefaultAsync(x => x.Estado);

                if (config == null)
                {
                    return Ok(new
                    {
                        existe = false
                    });
                }

                return Ok(new
                {
                    existe = true,

                    idConfig = config.IdConfig,

                    valorAccion = config.ValorAccion,

                    fechaInicio = config.FechaInicio,

                    fechaFin = config.FechaFin,

                    estado = config.Estado,

                    fechaRegistro = config.FechaRegistro
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
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

                // 1. Iniciamos Query con Includes tácticos asíncronos
                var query = _context.AportesSocios
                    .Include(a => a.Socio)
                    .Include(a => a.MedioPago)
                    .Where(a => a.EstadoPago != 'E') // Filtro perimetral: Excluir eliminados desde el inicio
                    .AsNoTracking()
                    .AsQueryable();

                // Variable de control para identificar si el socio tiene carga familiar asociada
                bool tieneNucleoFamiliar = false;

                // 2. FILTRO DE SEGURIDAD NÚCLEO FAMILIAR
                if (!isAdmin)
                {
                    int idSocioSesion = 0;
                    if (!string.IsNullOrEmpty(idSocioClaim)) int.TryParse(idSocioClaim, out idSocioSesion);

                    // 🛰️ RADAR DE FAMILIARIDAD: Buscamos los IDs de los familiares activos
                    var idsFamilia = await _context.Familiaridad
                        .Where(f => f.IdSocioTitular == idSocioSesion && f.Activo)
                        .Select(f => f.IdSocioFamiliar)
                        .ToListAsync();

                    if (idsFamilia.Count > 0)
                    {
                        tieneNucleoFamiliar = true;
                    }

                    // Incluimos al titular en el radio de búsqueda
                    idsFamilia.Add(idSocioSesion);

                    // Filtramos la query para que solo devuelva aportes autorizados del núcleo
                    query = query.Where(a => idsFamilia.Contains(a.IdSocio));
                }

                // ── 🎯 3. ARQUITECTURA DE ORDENAMIENTO EN CALIENTE (REGLA DE NEGOCIO V2026) ──
                if (isAdmin || tieneNucleoFamiliar)
                {
                    // Escenario Admin o Familia: Agrupamos visualmente por Socio y ordenamos del más actual al más antiguo
                    query = query
                        .OrderBy(a => a.Socio.Nombres)
                        .ThenBy(a => a.Socio.ApellidoPaterno)
                        .ThenBy(a => a.Socio.ApellidoMaterno)
                        .ThenByDescending(a => a.AnioAportado)  // 👈 Del más actual...
                        .ThenByDescending(a => a.MesAportado);   // 👈 ...al más antiguo
                }
                else
                {
                    // Socio Independiente: Línea de tiempo pura descendente
                    query = query
                        .OrderByDescending(a => a.AnioAportado)
                        .ThenByDescending(a => a.MesAportado);
                }

                // 4. Proyección de Datos Eficiente y Mapeo de Identidad Normalizado
                var aportes = await query
                    .Select(a => new {
                        idAporte = a.IdAporte,
                        idSocio = a.IdSocio,
                        // Concatenación robusta basada en la estructura contable real de tu base de datos
                        nombreSocio = $"{a.Socio.Nombres} {a.Socio.ApellidoPaterno} {a.Socio.ApellidoMaterno}".Trim().ToUpper(),
                        montoPagado = decimal.Round(a.MontoPagado, 2),
                        periodo = $"{a.MesAportado:00}-{a.AnioAportado}",
                        mesAportado = a.MesAportado,
                        anioAportado = a.AnioAportado,
                        medioPago = a.MedioPago != null ? a.MedioPago.Nombre.ToUpper() : "NO ESPECIFICADO",
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
        public async Task<IActionResult> ActualizarAporte(int id, [FromForm] AporteRequestDto dto)
        {
            var aporte = await _context.AportesSocios.FindAsync(id);
            if (aporte == null) return NotFound(new { message = "Aporte no encontrado." });

            if (aporte.EstadoPago == 'A')
                return BadRequest(new { message = "No se puede editar un aporte ya aprobado por tesorería." });

            // 🚀 BLINDAJE ULTRAESTRICTO: Validamos que REALMENTE exista un archivo binario nuevo y físico
            if (dto.ArchivoVoucher != null && dto.ArchivoVoucher.Length > 0 && !string.IsNullOrEmpty(dto.ArchivoVoucher.FileName))
            {
                try
                {
                    Console.WriteLine($"📡 [AZURE] Transmitiendo nuevo archivo: {dto.ArchivoVoucher.FileName} ({dto.ArchivoVoucher.Length} bytes)");
                    string nuevaUrlAzure = await _blobService.UploadVoucherAsync(dto.ArchivoVoucher);
                    aporte.UrlEvidencia = nuevaUrlAzure;
                }
                catch (System.Exception ex)
                {
                    // Devolvemos el error real del SDK de Azure para saber exactamente qué falló (Permisos, Llave, Contenedor)
                    return StatusCode(500, new { message = "Error de red al actualizar el voucher en Azure.", detalles = ex.Message, traza = ex.InnerException?.Message });
                }
            }
            else
            {
                // 🎯 Si el Front no mandó un archivo físico nuevo, NO tocamos Azure. 
                // Conservamos intacta la URL de la imagen que el aporte ya tenía guardada en la base de datos.
                Console.WriteLine("ℹ️ [AZURE] Edición sin cambio de imagen. Conservando URL previa.");
            }

            // Sincronización de los campos de negocio
            aporte.CantidadAcciones = dto.CantidadAcciones;
            aporte.MesAportado = dto.MesAportado;
            aporte.AnioAportado = dto.AnioAportado;
            aporte.IdMedioPago = dto.IdMedioPago;
            aporte.MontoPagado = dto.MontoPagado;

            if (aporte.EstadoPago == 'R') aporte.EstadoPago = 'P';

            await _context.SaveChangesAsync();
            return Ok(new { message = "Aporte actualizado correctamente.", url = aporte.UrlEvidencia });
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
                if (_context == null) throw new Exception("❌ El Contexto de Base de Datos es NULO");

                var lista = await _context.AportesSocios
                    .Include(a => a.Socio)
                    .Include(a => a.MedioPago)
                    .Include(a => a.ConfigAporte)
                    .Where(a => a.EstadoPago == 'P')
                    .ToListAsync();

                Console.WriteLine($"📊 REGISTROS ENCONTRADOS: {lista.Count}");

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
                    mesAportado = a.MesAportado,
                    anioAportado = a.AnioAportado,
                    medioPago = a.MedioPago != null ? a.MedioPago.Nombre : "NO ESPECIFICADO",
                    idMedioPago = a.IdMedioPago,
                    // 🎯 AJUSTE DIAMANTE: Se inyecta la propiedad al payload de salida
                    urlEvidencia = a.UrlEvidencia
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
        [HttpPost("actualizar-config")]
        public async Task<IActionResult> ActualizarConfiguracion( [FromBody] ConfigAporteRequestDTO request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 🛡️ VALIDACIONES ENTERPRISE

                if (request.ValorAccion <= 0)
                {
                    return BadRequest(new
                    {
                        message = "El valor de acción debe ser mayor a cero."
                    });
                }

                if (request.FechaInicio == DateTime.MinValue)
                {
                    return BadRequest(new
                    {
                        message = "Debe ingresar fecha inicio."
                    });
                }

                if (request.FechaFin.HasValue &&
                    request.FechaFin.Value < request.FechaInicio)
                {
                    return BadRequest(new
                    {
                        message = "La fecha fin no puede ser menor a la fecha inicio."
                    });
                }

                // 🔍 BUSCAR CONFIGURACIÓN VIGENTE
                var vigente = await _context.ConfigAportes
                    .FirstOrDefaultAsync(x =>
                        x.Estado &&
                        x.FechaFin == null);

                // 🚀 CERRAR CONFIGURACIÓN ANTERIOR
                if (vigente != null)
                {
                    vigente.Estado = false;

                    vigente.FechaFin =
                        request.FechaInicio.AddDays(-1);

                    vigente.FechaModificacion = DateTime.Now;

                    vigente.IdUsuarioModificacion = int.Parse(
                        User.FindFirst("IdUsuario")?.Value ?? "0"
                    );
                }

                // 👤 USUARIO LOGUEADO
                var idUsuario = int.Parse(
                    User.FindFirst("IdUsuario")?.Value ?? "0"
                );

                // 🚀 NUEVA CONFIGURACIÓN
                var nueva = new ConfigAporte
                {
                    ValorAccion = request.ValorAccion,

                    FechaInicio = request.FechaInicio,

                    FechaFin = request.FechaFin,

                    Estado = request.Estado,

                    FechaRegistro = DateTime.Now,

                    IdUsuarioRegistro = idUsuario
                };

                _context.ConfigAportes.Add(nueva);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "Nueva vigencia configurada correctamente."
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("consolidado")]
        public async Task<IActionResult> GetConsolidado([FromQuery] int? anio)
        {
            // ── 🎯 EXTRACCIÓN HIGHER-SPEC DE CLAIMS DEL TOKEN JWT ──
            var idSocioToken = User.FindFirst("IdSocio")?.Value;
            var idUsuarioToken = User.FindFirst("IdUsuario")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Evaluamos el rol buscando tanto en los claims estándar como en tus mapeos de texto del JSON ("Perfil" y "role")
            var perfilClaim = User.FindFirst("Perfil")?.Value ?? User.FindFirst("role")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";

            // ── 🎯 BLINDAJE LOGÍSITICO ANTI-FALSOS NEGATIVOS ──
            bool esAdmin = User.IsInRole("Admin")
                        || User.IsInRole("Administrador")
                        || User.IsInRole("Gerente")
                        || new[] { "admin", "administrador", "gerente" }.Contains(perfilClaim.ToLower().Trim());

            int socioId = 0;
            if (!string.IsNullOrWhiteSpace(idSocioToken))
            {
                int.TryParse(idSocioToken, out socioId);
            }

            int usuarioId = 0;
            if (!string.IsNullOrWhiteSpace(idUsuarioToken))
            {
                int.TryParse(idUsuarioToken, out usuarioId);
            }

            // Invocamos el servicio pasando la bandera real validada por espectro de texto
            var reporte = await _aporteService.ObtenerReporteConsolidadoAsync(socioId, usuarioId, esAdmin, anio);

            return Ok(reporte);
        }
        // Se mantienen intactos los métodos Aprobar, Rechazar, GetConfig y GetPendientes...
    }
}
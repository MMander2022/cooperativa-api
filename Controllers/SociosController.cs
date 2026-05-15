using CooperativaApp.Data;
using CooperativaApp.DTOS;
using CooperativaApp.Models;
using CooperativaApp.Services;
using CooperativaApp.Interfaces; // 🛰️ Importante para los servicios de mensajería
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CooperativaApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 🛡️ Bloqueo de acceso no autorizado centralizado
    public class SociosController : ControllerBase
    {
        private readonly CooperativaContext _context;
        private readonly IAuthService _authService;
        // 🛡️ Motores de Notificación Inyectados
        private readonly IEmailService _emailService;
        private readonly IWhatsAppService _whatsappService;
        private readonly ISmsService _smsService;
        public SociosController(CooperativaContext context,
            IAuthService authService,
            IEmailService emailService,
            IWhatsAppService whatsappService,
            ISmsService smsService)
        {
            _context = context;
            _authService = authService;
            _emailService = emailService;
            _whatsappService = whatsappService;
            _smsService = smsService;
        }

        // 🔍 VALIDACIÓN DNI (Optimizado con AsNoTracking)
        [HttpGet("check-dni/{dni}")]
        public async Task<IActionResult> CheckDNI(string dni)
        {
            var socio = await _context.Socios
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.DNI == dni); // 🚀 Quitamos el Select limitado para traer TODO

            return Ok(new { exists = socio != null, data = socio });
        }

        // 📋 LISTADO GENERAL
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Socio>>> GetSocios()
        {
            return await _context.Socios
                .AsNoTracking()
                .OrderByDescending(s => s.IdSocio)
                .ToListAsync();
        }
        [HttpGet("lista-selector")]
        [Authorize]
        public async Task<IActionResult> GetSociosSelector()
        {
            try
            {
                // 🚀 MÉTODO TITANIUM: Concatenación simple compatible con SQL
                var socios = await _context.Socios
                    .Where(s => s.Estado == true) // Asegúrate que 'Estado' sea bool
                    .Select(s => new {
                        IdSocio = s.IdSocio,
                        // Usamos interpolación simple o concatenación con '+'
                        NombreCompleto = s.Nombres + " " + s.Apellidos
                    })
                    .OrderBy(s => s.NombreCompleto)
                    .ToListAsync();

                return Ok(socios);
            }
            catch (Exception ex)
            {
                // Log para rastrear si el error persiste en el hangar
                Console.WriteLine($"💥 Error en lista-selector: {ex.Message}");
                return StatusCode(500, new { message = "Error al recuperar la lista de socios." });
            }
        }
        [HttpPost]
        public async Task<ActionResult<Socio>> CreateSocio([FromBody] Socio socio)
        {
            try
            {
                // 🚀 Delegamos toda la lógica al método Titanium
                var nuevoSocio = await RegistrarSocioTitanium(socio);
                return CreatedAtAction("GetSocio", new { id = nuevoSocio.IdSocio }, nuevoSocio);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error crítico en el despliegue: {ex.Message}");
            }
        }
        // 🔄 ACTUALIZAR SOCIO
        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(int id, Socio socio)
        {
            if (id != socio.IdSocio) return BadRequest("ID de ruta no coincide con ID de objeto.");

            var socioBD = await _context.Socios.FindAsync(id);
            if (socioBD == null) return NotFound($"Socio ID {id} no existe.");

            // 🛡️ No permitir cambiar el DNI si ya existe otro socio con ese número
            if (socioBD.DNI != socio.DNI && await _context.Socios.AnyAsync(s => s.DNI == socio.DNI))
                return BadRequest("El nuevo DNI ya está asignado a otro socio.");

            // Mapeo Selectivo Pro
            socioBD.Nombres = socio.Nombres;
            socioBD.ApellidoPaterno = socio.ApellidoPaterno;
            socioBD.ApellidoMaterno = socio.ApellidoMaterno;
            socioBD.Apellidos = $"{socio.ApellidoPaterno} {socio.ApellidoMaterno}".Trim();
            socioBD.DNI = socio.DNI;
            socioBD.Telefono = socio.Telefono;
            socioBD.correo = socio.correo;
            socioBD.Direccion = socio.Direccion;
            socioBD.FechaNacimiento = socio.FechaNacimiento;
            socioBD.Estado = socio.Estado;
            socioBD.IdUsuarioModificacion = GetCurrentUserId();
            socioBD.FechaModificacion = DateTime.Now; // Auditoría temporal

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Cambios guardados exitosamente." });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict("Los datos fueron modificados por otro usuario. Recargue la página.");
            }
        }
        /// <summary>
        /// 🚪 BAJA LÓGICA TITANIUM: Acción con auditoría y validación de seguridad
        /// </summary>
        [HttpPost("baja-logica")]
        public async Task<IActionResult> BajaLogica([FromBody] BajaSocioDTO dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { message = "Protocolo de seguridad: Sesión inválida." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var socio = await _context.Socios.FindAsync(dto.IdSocio);
                if (socio == null) return NotFound(new { message = "Socio no localizado." });
                if (!socio.Estado) return BadRequest(new { message = "El socio ya se encuentra inactivo." });

                // 🛡️ ESCUDO FINAL: Re-validación técnica en la transacción
                bool tieneDeudas = await _context.Creditos.AnyAsync(c =>
                    c.IdSocio == dto.IdSocio &&
                    c.Estado.Trim().ToUpper() == "DESEMBOLSADO" &&
                    (c.EstadoCredito.Trim().ToUpper() == "VIGENTE" || c.EstadoCredito.Trim().ToUpper() == "ACTIVO")
                );

                if (tieneDeudas)
                    return BadRequest(new { message = "Acceso denegado: El socio posee créditos vigentes no detectados previamente." });

                // 📝 Registro de Historial
                var historial = new HistorialEstadoSocio
                {
                    IdSocio = dto.IdSocio,
                    IdUsuarioAccion = userId,
                    EstadoAnterior = true,
                    EstadoNuevo = false,
                    IdMotivo = dto.IdMotivo,
                    Comentario = string.IsNullOrWhiteSpace(dto.Comentario) ? "Cese de actividades estándar" : dto.Comentario,
                    FechaAccion = DateTime.Now
                };

                // 🔄 Actualización de Matriz
                socio.Estado = false;
                socio.IdUsuarioModificacion = userId;
                socio.FechaModificacion = DateTime.Now;

                _context.HistorialEstadoSocio.Add(historial);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Protocolo de baja procesado y auditado con éxito." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Fallo crítico en el motor de baja", detail = ex.Message });
            }
        }
        // 🗑️ ELIMINACIÓN CON REGLA DE NEGOCIO
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var socio = await _context.Socios.FindAsync(id);
            if (socio == null) return NotFound();

            // 🚫 REGLA DE ORO: No se borra si debe dinero
            bool tieneDeudas = await _context.Creditos.AnyAsync(c => c.IdSocio == id && c.Estado  == "Activo");
            if (tieneDeudas)
                return BadRequest("No se puede desactivar un socio con créditos vigentes.");

            socio.Estado = false;
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Socio desactivado." });
        }

        [HttpGet("motivos-baja")]
        public async Task<IActionResult> GetMotivosBaja()
        {
            return Ok(await _context.MotivoBaja
                .AsNoTracking()
                .Where(m => m.Activo == true)
                .ToListAsync());
        }
        [HttpPost("RegistrarSocioTitanium")]
        public async Task<Socio> RegistrarSocioTitanium(Socio socio)
        {
            int adminId = GetCurrentUserId();

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Normalización de Datos
                    if (string.IsNullOrWhiteSpace(socio.Apellidos))
                    {
                        socio.Apellidos = $"{socio.ApellidoPaterno} {socio.ApellidoMaterno}".Trim();
                    }

                    var perfilSocio = await _context.Perfiles
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Nombre == "Socio");

                    if (perfilSocio == null)
                        throw new Exception("El perfil 'Socio' no existe en la base de datos.");

                    // 2. Auditoría y Guardado de Socio
                    socio.IdUsuarioRegistro = adminId;
                    socio.FechaRegistro = DateTime.Now;
                    socio.Estado = true;

                    _context.Socios.Add(socio);
                    await _context.SaveChangesAsync();

                    // 3. Generación de Credenciales Temporales
                    // Usamos los primeros 4 dígitos del DNI para la clave temporal
                    string passwordTemporal = $"Socio{socio.DNI.Substring(0, 4)}*";
                    var (hash, salt) = await _authService.HashearPassword(passwordTemporal);

                    // 4. Crear el Usuario vinculado
                    var nuevoUsuario = new Usuario
                    {
                        IdSocio = socio.IdSocio,
                        Username = socio.DNI,
                        PasswordHash = hash,
                        PasswordSalt = salt,
                        NombreCompleto = $"{socio.Nombres} {socio.ApellidoPaterno}",
                        Email = socio.correo,
                        IdPerfil = perfilSocio.IdPerfil,
                        Estado = true,
                        RequiereCambioPassword = true, // 🚩 Bloqueo activado para el primer login
                        FechaCreacion = DateTime.Now,
                        IsLocked = false
                    };

                    _context.Usuarios.Add(nuevoUsuario);
                    await _context.SaveChangesAsync();

                    // 🏁 CIERRE DE TRANSACCIÓN EN BD
                    await transaction.CommitAsync();

                    // 🛰️ DISPARO DE NOTIFICACIONES TITANIUM (Post-Commit)
                    // Construimos el mensaje de bienvenida
                    string mensajeBienvenida = $@"🌟 ¡Bienvenido a UNIMAS, {socio.Nombres}! 
                    Tu cuenta ha sido creada con éxito.
                    👤 Usuario: {socio.DNI}
                    🔑 Clave Temporal: {passwordTemporal}
                    
                    ⚠️ Por seguridad, deberás cambiar esta clave al ingresar por primera vez.";

                    // Disparamos notificaciones (Fire and forget o await según prefieras)
                    // Nota: Se recomienda Task.Run o no esperar para no bloquear la respuesta del API
                    _ = Task.Run(async () => {
                        try
                        {
                            await _emailService.SendEmailAsync(socio.correo, "Bienvenida UNIMAS - Activación de Cuenta", mensajeBienvenida);
                            await _whatsappService.SendWhatsAppAsync(socio.Telefono, mensajeBienvenida);
                            await _smsService.SendSmsAsync(socio.Telefono, $"Bienvenido {socio.Nombres}. Tu clave temporal es: {passwordTemporal}");
                        }
                        catch (Exception ex)
                        {
                            // Aquí podrías loguear que la notificación falló, pero el socio ya está creado
                            Console.WriteLine($"⚠️ Error en el radar de notificaciones: {ex.Message}");
                        }
                    });

                    return socio;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
        // 1. 🛰️ AÑADIR ESTE MÉTODO (Es la pieza que faltaba en el rompecabezas)
        [HttpGet("{id}")]
        public async Task<ActionResult<Socio>> GetSocio(int id)
        {
            var socio = await _context.Socios.FindAsync(id);

            if (socio == null)
            {
                return NotFound();
            }

            return socio;
        }
        private int GetCurrentUserId()
        {
            //var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            //return claim != null && int.TryParse(claim.Value, out int userId) ? userId : 0;

            var claim = User.FindFirst("IdUsuario") ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null && int.TryParse(claim.Value, out int userId))
            {
                return userId;
            }

            return 0;
        }
        [HttpGet("{id}/situacion-crediticia")]
        public async Task<IActionResult> GetSituacionCrediticia(int id)
        {
            // 🛡️ REGLA DE ORO: Normalización de búsqueda para evitar fallos de escritura
            bool tieneDeudas = await _context.Creditos.AnyAsync(c =>
                c.IdSocio == id &&
                c.Estado.Trim().ToUpper() == "DESEMBOLSADO" &&
                (c.EstadoCredito.Trim().ToUpper() == "VIGENTE" || c.EstadoCredito.Trim().ToUpper() == "ACTIVO")
            );

            return Ok(new
            {
                tieneDeuda = tieneDeudas,
                mensaje = tieneDeudas
                    ? "BLOQUEO: El socio posee créditos desembolsados con estado vigente/activo."
                    : "Socio sin obligaciones pendientes. Protocolo de baja permitido."
            });
        }
        [HttpGet("mis-creditos")]
        public async Task<IActionResult> GetMisCreditos()
        {
            var userId = int.Parse(User.FindFirst("IdUsuario")?.Value ?? "0");
            var userPerfil = User.FindFirst("Perfil")?.Value;
            var socioIdStr = User.FindFirst("IdSocio")?.Value;

            // 🕵️ Radar de Perfiles
            var query = _context.Creditos
                .Include(c => c.Socio)
                .Include(c => c.Cuotas)
                .Where(c => new[] { "APROBADO", "DESEMBOLSADO", "PARCIAL", "VIGENTE" }.Contains(c.Estado))
                .AsQueryable();

            // Si NO es Admin, filtramos estrictamente por su IdSocio
            if (userPerfil != "Administrador" && !string.IsNullOrEmpty(socioIdStr))
            {
                int socioId = int.Parse(socioIdStr);
                query = query.Where(c => c.IdSocio == socioId);
            }

            var lista = await query.Select(c => new {
                c.IdCredito,
                c.Monto,
                c.FechaAprobacion,
                c.Estado,
                NombreSocio = c.Socio.Nombres + " " + c.Socio.Apellidos,
                // 💎 Información Amigable para el Socio
                ProximoVencimiento = c.Cuotas
                    .Where(q => q.Estado != "PAGADO")
                    .OrderBy(q => q.NumeroCuota)
                    .Select(q => (DateTime?)q.FechaVencimiento)
                    .FirstOrDefault(),
                SaldoPendiente = c.Cuotas
                    .Where(q => q.Estado != "PAGADO")
                    .Sum(q => q.Saldo)
            }).ToListAsync();

            return Ok(lista);
        }
        [HttpGet("mis-familiares")]
        public async Task<IActionResult> GetMisFamiliares()
        {
            try
            {
                // 🛡️ Identidad del titular en sesión
                var idSocioClaim = User.FindFirst("IdSocio")?.Value;
                if (string.IsNullOrEmpty(idSocioClaim)) return Unauthorized();

                int idSocioSesion = int.Parse(idSocioClaim);

                // 🛰️ RADAR DE NÚCLEO: Buscamos a los familiares vinculados
                var familiares = await _context.Familiaridad
                    .Where(f => f.IdSocioTitular == idSocioSesion && f.Activo)
                    .Include(f => f.SocioFamiliar) // Traemos la data del familiar
                    .Select(f => new {
                        idSocio = f.IdSocioFamiliar,
                        nombreCompleto = $"{f.SocioFamiliar.Nombres} {f.SocioFamiliar.Apellidos}".ToUpper()
                    })
                    .ToListAsync();

                // 🎯 IMPORTANTE: Incluimos al Titular mismo en la lista
                var titular = await _context.Socios
                    .Where(s => s.IdSocio == idSocioSesion)
                    .Select(s => new {
                        idSocio = s.IdSocio,
                        nombreCompleto = $"{s.Nombres} {s.Apellidos} (TITULAR)".ToUpper()
                    })
                    .FirstOrDefaultAsync();

                if (titular != null) familiares.Insert(0, titular);

                return Ok(familiares);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error al recuperar núcleo familiar: {ex.Message}" });
            }
        }
    }
}
using CooperativaApp.Data;
using CooperativaApp.DTOS;
using CooperativaApp.Interfaces;
using CooperativaApp.Models;
using CooperativaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CooperativaApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUsuarioRepository _repo;
        private readonly IAuditoriaService _audit;
        private readonly CooperativaContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, IUsuarioRepository repo, IAuditoriaService audit, CooperativaContext context, ILogger<AuthController> logger)
        {
            _authService = authService;
            _repo = repo;
            _audit = audit;
            _context = context;
            _logger = logger;
        }
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            // 1. Localización de identidad previa
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Username == dto.Username);

            if (usuario == null)
                return BadRequest(new { Message = "El usuario no se encuentra en el radar del sistema." });

            // 2. Verificación de sellado (Bloqueo)
            if (usuario.IsLocked)
                return BadRequest(new { Message = "Cuenta bloqueada por seguridad. Contacte al administrador del núcleo." });

            try
            {
                // 3. 🚀 LLAMADA AL MOTOR UNIFICADO
                var response = await _authService.Authenticate(dto.Username, dto.Password);

                // 4. ÉXITO: Resetear telemetría de fallos
                if (usuario.IntentosFallidos > 0)
                {
                    usuario.IntentosFallidos = 0;
                    await _context.SaveChangesAsync();
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                // 🛡️ LÓGICA DE INTENTOS FALLIDOS (Protocolo de Bloqueo)
                if (ex.Message.Contains("incorrectas") || ex.Message.Contains("detectado"))
                {
                    usuario.IntentosFallidos += 1;
                    int restantes = 3 - usuario.IntentosFallidos;

                    try
                    {
                        if (usuario.IntentosFallidos >= 3)
                        {
                            usuario.IsLocked = true;
                            await _context.SaveChangesAsync();
                            return BadRequest(new { Message = "Acceso denegado. Cuenta bloqueada tras 3 intentos fallidos." });
                        }

                        await _context.SaveChangesAsync();
                        return BadRequest(new { Message = $"Contraseña incorrecta. Le quedan {restantes} intentos antes del bloqueo." });
                    }
                    catch (InvalidOperationException)
                    {
                        // 💎 MANIOBRA DE EMERGENCIA: Re-inicializar ConnectionString si EF lo perdió
                        _context.Database.GetDbConnection().ConnectionString = _context.Database.GetConnectionString();
                        await _context.SaveChangesAsync();

                        return BadRequest(new { Message = $"Contraseña incorrecta. Le quedan {restantes} intentos." });
                    }
                }

                // Error técnico no controlado (Falla de motor o DB)
                return BadRequest(new { Message = $"Falla en el núcleo: {ex.Message}" });
            }
        }
        [AllowAnonymous]
        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar(UsuarioRegistroDto dto)
        {
            if (await _context.Usuarios.AnyAsync(u => u.Username == dto.Username))
                return BadRequest(new { Message = "El nombre de usuario ya está en uso." });

            var (hash, salt) = await _authService.HashearPassword(dto.Password);

            var nuevoUsuario = new Usuario
            {
                Username = dto.Username,
                PasswordHash = hash,
                PasswordSalt = salt,
                NombreCompleto = dto.NombreCompleto,
                RequiereCambioPassword = true,
                Email = dto.Email,
                IdPerfil = dto.IdPerfil,
                Estado = true,
                IntentosFallidos = 0,
                IsLocked = false
            };

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            await _audit.RegistrarLog(nuevoUsuario.IdUsuario, "USER_CREATED", $"Usuario {dto.Username} creado", Request.HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok(new { Message = "Usuario creado con éxito. Ya puede iniciar sesión." });
        }
        [HttpPost("cambiar-password-obligatorio")]
        [AllowAnonymous] // O [Authorize] si prefieres validar el token temporal
        public async Task<IActionResult> CambiarPasswordObligatorio([FromBody] ResetPasswordDTO dto)
        {
            try
            {
                // 1. Buscar al usuario en la base de datos
                var usuario = await _context.Usuarios.FindAsync(dto.IdUsuario);

                if (usuario == null)
                    return NotFound("Usuario no detectado en el sistema.");

                // 2. Generar Nuevo Hash y Salt Titanium
                // Usamos el mismo servicio que ya configuramos antes
                var (nuevoHash, nuevoSalt) = await _authService.HashearPassword(dto.NuevaPassword);

                // 3. Actualizar Credenciales y Apagar el Flag de Cambio
                usuario.PasswordHash = nuevoHash;
                usuario.PasswordSalt = nuevoSalt;
                usuario.RequiereCambioPassword = false; // 🔓 ¡Acceso Total Activado!
                usuario.UltimoLogin = DateTime.Now;

                // 4. Guardar Cambios
                _context.Usuarios.Update(usuario);
                await _context.SaveChangesAsync();

                return Ok("Contraseña actualizada con éxito. Protocolo Titanium completado.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Fallo en la actualización: {ex.Message}");
            }
        }
    }
}
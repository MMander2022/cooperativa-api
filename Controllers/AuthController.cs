using CooperativaApp.Data;
using CooperativaApp.DTOS;
using CooperativaApp.Interfaces;
using CooperativaApp.Models;
using CooperativaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
        private readonly IConfiguration _config;

        public AuthController(
            IAuthService authService,
            IUsuarioRepository repo,
            IAuditoriaService audit,
            CooperativaContext context,
            ILogger<AuthController> logger,
            IConfiguration config)
        {
            _authService = authService;
            _repo = repo;
            _audit = audit;
            _context = context;
            _logger = logger;
            _config = config;
        }

        // ─────────────────────────────────────────
        // POST api/auth/login
        // ─────────────────────────────────────────
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { Message = "Datos de acceso incompletos." });

            const string mensajeGenerico = "Credenciales incorrectas.";
            int maxIntentos = _config.GetValue<int>("SecuritySettings:MaxIntentosFallidos", 3);
            int delayMs = _config.GetValue<int>("SecuritySettings:LoginDelayMs", 300);
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Username == dto.Username);

            // Anti timing-attack: mismo tiempo de respuesta aunque el usuario no exista
            if (usuario == null)
            {
                await Task.Delay(delayMs);
                _logger.LogWarning("Intento de login con usuario inexistente: {Username} desde {IP}", dto.Username, ip);
                return BadRequest(new { Message = mensajeGenerico });
            }

            if (usuario.IsLocked)
            {
                _logger.LogWarning("Intento de acceso a cuenta bloqueada: {Username} desde {IP}", dto.Username, ip);
                return BadRequest(new { Message = "Cuenta bloqueada. Contacte al administrador." });
            }

            try
            {
                var response = await _authService.Authenticate(dto.Username, dto.Password);

                // Login exitoso — resetear contadores
                usuario.IntentosFallidos = 0;
                usuario.UltimoLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await _audit.RegistrarLog(
                    usuario.IdUsuario,
                    "LOGIN_SUCCESS",
                    $"Inicio de sesión exitoso para {dto.Username}",
                    ip);

                _logger.LogInformation("Login exitoso: {Username} desde {IP}", dto.Username, ip);
                return Ok(response);
            }
            catch (Exception ex) when (ex.Message.Contains("incorrectas") || ex.Message.Contains("detectado"))
            {
                usuario.IntentosFallidos += 1;
                int restantes = maxIntentos - usuario.IntentosFallidos;

                if (usuario.IntentosFallidos >= maxIntentos)
                {
                    usuario.IsLocked = true;
                    await _context.SaveChangesAsync();

                    await _audit.RegistrarLog(
                        usuario.IdUsuario,
                        "ACCOUNT_LOCKED",
                        $"Cuenta bloqueada tras {maxIntentos} intentos fallidos",
                        ip);

                    _logger.LogWarning("Cuenta bloqueada: {Username} desde {IP}", dto.Username, ip);
                    return BadRequest(new { Message = "Cuenta bloqueada. Contacte al administrador." });
                }

                await _context.SaveChangesAsync();

                await _audit.RegistrarLog(
                    usuario.IdUsuario,
                    "LOGIN_FAILED",
                    $"Intento fallido {usuario.IntentosFallidos}/{maxIntentos}",
                    ip);

                return BadRequest(new { Message = $"{mensajeGenerico} Intentos restantes: {restantes}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error técnico en login para {Username}", dto.Username);
                return StatusCode(500, new { Message = "Error interno. Intente más tarde." });
            }
        }

        // ─────────────────────────────────────────
        // POST api/auth/registrar
        // ─────────────────────────────────────────
        [AllowAnonymous]
        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] UsuarioRegistroDto dto)
        {
            if (dto is null)
                return BadRequest(new { Message = "Datos de registro incompletos." });

            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { Message = "Usuario y contraseña son obligatorios." });

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

            // Verificar duplicado sin revelar si existe (mismo mensaje)
            if (await _context.Usuarios.AnyAsync(u => u.Username == dto.Username))
                return Conflict(new { Message = "No se pudo completar el registro. Verifique los datos." });

            var (hash, salt) = await _authService.HashearPassword(dto.Password);

            var nuevoUsuario = new Usuario
            {
                Username = dto.Username.Trim(),
                PasswordHash = hash,
                PasswordSalt = salt,
                NombreCompleto = dto.NombreCompleto?.Trim(),
                Email = dto.Email?.Trim().ToLowerInvariant(),
                IdPerfil = dto.IdPerfil,
                RequiereCambioPassword = true,
                Estado = true,
                IntentosFallidos = 0,
                IsLocked = false
            };

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            await _audit.RegistrarLog(
                nuevoUsuario.IdUsuario,
                "USER_CREATED",
                $"Usuario {dto.Username} registrado",
                ip);

            _logger.LogInformation("Nuevo usuario registrado: {Username}", dto.Username);
            return Ok(new { Message = "Usuario creado con éxito. Ya puede iniciar sesión." });
        }

        // ─────────────────────────────────────────
        // POST api/auth/cambiar-password-obligatorio
        // ─────────────────────────────────────────
        [AllowAnonymous]
        [HttpPost("cambiar-password-obligatorio")]
        public async Task<IActionResult> CambiarPasswordObligatorio([FromBody] ResetPasswordDTO dto)
        {
            if (dto is null || dto.IdUsuario <= 0 || string.IsNullOrWhiteSpace(dto.NuevaPassword))
                return BadRequest(new { Message = "Datos incompletos para el cambio de contraseña." });

            int minimaLongitud = _config.GetValue<int>("SecuritySettings:PasswordMinLength", 8);

            if (dto.NuevaPassword.Length < minimaLongitud)
                return BadRequest(new { Message = $"La contraseña debe tener al menos {minimaLongitud} caracteres." });

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

            var usuario = await _context.Usuarios.FindAsync(dto.IdUsuario);
            if (usuario == null)
                return NotFound(new { Message = "Usuario no encontrado." });

            if (!usuario.RequiereCambioPassword)
                return BadRequest(new { Message = "Este usuario no tiene un cambio de contraseña pendiente." });

            var (nuevoHash, nuevoSalt) = await _authService.HashearPassword(dto.NuevaPassword);

            usuario.PasswordHash = nuevoHash;
            usuario.PasswordSalt = nuevoSalt;
            usuario.RequiereCambioPassword = false;
            usuario.UltimoLogin = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _audit.RegistrarLog(
                usuario.IdUsuario,
                "PASSWORD_CHANGED",
                "Cambio de contraseña obligatorio completado",
                ip);

            _logger.LogInformation("Password cambiado para usuario ID {IdUsuario}", dto.IdUsuario);
            return Ok(new { Message = "Contraseña actualizada con éxito." });
        }
    }
}
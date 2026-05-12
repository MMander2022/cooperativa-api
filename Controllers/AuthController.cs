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
        private readonly IConfiguration _config;

        public AuthController(IAuthService authService, IUsuarioRepository repo, IAuditoriaService audit, CooperativaContext context, ILogger<AuthController> logger, IConfiguration config)
        {
            _authService = authService;
            _repo = repo;
            _audit = audit;
            _context = context;
            _logger = logger;
            _config = config;
        }
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
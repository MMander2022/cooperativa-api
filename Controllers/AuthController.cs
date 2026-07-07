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
        public async Task<IActionResult> Registrar([FromBody] UsuarioRegistroDto dto)
        {
            if (dto is null)
                return BadRequest(new { Message = "Datos de registro incompletos." });

            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { Message = "Usuario y contraseña son obligatorios." });

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

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
        [AllowAnonymous]
        [HttpPost("recuperar-credenciales")]
        public async Task<IActionResult> RecuperarCredenciales([FromBody] RecuperarAccountDTO dto, [FromServices] IEmailService emailService)
        {
            // 🛡️ Blindaje de entrada
            if (dto is null || string.IsNullOrWhiteSpace(dto.Dni))
                return BadRequest(new { Message = "El número de documento (DNI) es requerido." });

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

            // 1. Validar inyección del contexto de BD
            if (_context == null || _context.Usuarios == null)
                return StatusCode(500, new { Message = "Error interno: El contexto de base de datos no está inicializado." });

            // 🛰️ RADAR DE IDENTIDAD POR DNI CON INCLUDE EXPLICITO (Sana el NullReferenceException)
            var usuario = await _context.Usuarios
                .Include(u => u.Socio) // 👈 CRÍTICO: Fuerza la carga de la relación de socios
                .FirstOrDefaultAsync(u => u.Socio != null && u.Socio.DNI == dto.Dni.Trim());

            if (usuario == null)
            {
                return NotFound(new { Message = "El número de documento ingresado no se encuentra registrado." });
            }

            // Doble verificación defensiva de la relación
            if (usuario.Socio == null)
            {
                return BadRequest(new { Message = "El usuario existe pero no cuenta con un expediente de socio vinculado." });
            }

            // 🛡️ VALIDACIÓN CRÍTICA: Prevenir el nulo si el socio no tiene correo en la base de datos
            if (string.IsNullOrWhiteSpace(usuario.Email))
            {
                return BadRequest(new { Message = "El usuario existe, pero no cuenta con un correo electrónico válido registrado en el sistema." });
            }

            // 2. Validar que el generador de auth no sea nulo
            if (_authService == null)
                return StatusCode(500, new { Message = "Error interno: El servicio de autenticación (_authService) es nulo." });

            // Generar Contraseña Temporal Criptográfica Única
            string passwordTemporal = GenerarPasswordAleatorioTop(10);
            var (nuevoHash, nuevoSalt) = await _authService.HashearPassword(passwordTemporal);

            usuario.PasswordHash = nuevoHash;
            usuario.PasswordSalt = nuevoSalt;
            usuario.RequiereCambioPassword = true;

            await _context.SaveChangesAsync();

            // 3. Validar el despachador de correos inyectado por parámetro
            if (emailService == null)
            {
                return StatusCode(500, new { Message = "Error interno: El servicio IEmailService no pudo ser inyectado. Verifique Program.cs." });
            }

            // 4. Despacho al buzón específico
            string asunto = "Plataforma UNIMAS - Restablecimiento de Cuenta";
            string cuerpoHtml = $@"
    <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 12px;'>
        <h2 style='color: #0C447C;'>Restauración de Cuenta UNIMAS</h2>
        <p>Estimado socio, se ha procesado el restablecimiento de su cuenta asociada al DNI: <strong>{usuario.Socio.DNI}</strong>.</p>
        <div style='background-color: #f8fafc; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #2563eb;'>
            <p style='margin: 5px 0;'><strong>Su Nombre de Usuario:</strong> <span style='color: #0f172a; font-weight: bold;'>{usuario.Username}</span></p>
            <p style='margin: 5px 0;'><strong>Contraseña Temporal:</strong> <code style='color: #dc2626; font-size: 16px; font-weight: bold;'>{passwordTemporal}</code></p>
        </div>
        <p style='color: #475569; font-size: 13px;'>Al ingresar a la plataforma con esta clave provisional, el sistema le solicitará actualizarla obligatoriamente.</p>
        <hr style='border: 0; border-top: 1px solid #e2e8f0; margin: 20px 0;' />
        <small style='color: #94a3b8;'>Módulo de Seguridad UNIMAS.</small>
    </div>";

            // Usando tu método nativo validado del backend
            await emailService.SendEmailAsync(usuario.Email, asunto, cuerpoHtml);

            // 5. Validar inyección del logger o log de auditoría antes de disparar
            if (_audit != null)
            {
                await _audit.RegistrarLog(usuario.IdUsuario, "PASSWORD_RESET_DNI", $"Clave provisional despachada al buzón.", ip);
            }

            string emailEnmascarado = EnmascararEmail(usuario.Email);

            return Ok(new
            {
                Message = "¡Restablecimiento Iniciado!",
                Detalle = $"Hemos generado un acceso temporal único y lo enviamos al correo electrónico registrado para este documento: {emailEnmascarado}."
            });
        }
        private string EnmascararEmail(string email)
        {
            try
            {
                var partes = email.Split('@');
                if (partes[0].Length <= 2) return $"*@{partes[1]}";
                return $"{partes[0].Substring(0, 1)}*******{partes[0].Substring(partes[0].Length - 1)}@{partes[1]}";
            }
            catch { return "su correo registrado"; }
        }

        private string GenerarPasswordAleatorioTop(int longitud)
        {
            const string caracteres = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!@*#";
            var token = new char[longitud];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[longitud];
                rng.GetBytes(bytes);
                for (int i = 0; i < longitud; i++)
                {
                    token[i] = caracteres[bytes[i] % caracteres.Length];
                }
            }
            return new string(token);
        }
        // DTO necesario para el mapeo del request
        public class RecuperarAccountDTO
        {
            public string? Username { get; set; }
            public string? Email { get; set; }
            public string Dni { get; set; }
        }
    }
}
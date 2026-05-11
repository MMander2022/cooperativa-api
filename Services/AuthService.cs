using CooperativaApp.Interfaces;
using CooperativaApp.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CooperativaApp.DTOS;
using CooperativaApp.Data;
using Microsoft.EntityFrameworkCore;
using Dapper;
using System.Data;

namespace CooperativaApp.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _config;
        private readonly CooperativaContext _context; // 🛡️ Inyectado correctamente
        private readonly ILogger<AuthService> _logger;

        public AuthService(IConfiguration config, ILogger<AuthService> logger, CooperativaContext context)
        {
            _config = config;
            _logger = logger;
            _context = context;
        }
        public async Task<LoginResponseDto> Authenticate(string username, string password)
        {
            _logger.LogInformation("🔍 Escaneando identidad en el núcleo: {Username}", username);

            var connection = _context.Database.GetDbConnection();
            if (string.IsNullOrEmpty(connection.ConnectionString))
            {
                connection.ConnectionString = _context.Database.GetConnectionString();
            }

            if (connection.State == ConnectionState.Closed) await connection.OpenAsync();

            try
            {
                using (var multi = await connection.QueryMultipleAsync("sp_AutenticarUsuarioTitanium",
                                    new { Username = username },
                                    commandType: CommandType.StoredProcedure))
                {
                    // 1. LEER TODOS LOS RESULTADOS DE INMEDIATO (Vaciado de Buffer)
                    // Extraemos usuario y menú ANTES de cualquier validación lógica
                    var userData = await multi.ReadFirstOrDefaultAsync<dynamic>();

                    // Si el reader aún tiene datos, leemos el menú; si no, lista vacía
                    var menuData = !multi.IsConsumed
                        ? (await multi.ReadAsync<MenuDto>()).ToList()
                        : new List<MenuDto>();

                    // 2. VALIDACIÓN DE EXISTENCIA
                    if (userData == null)
                    {
                        _logger.LogWarning("❌ Usuario {User} no detectado.", username);
                        throw new Exception("El usuario no se encuentra en el radar del sistema.");
                    }

                    // 3. VERIFICACIÓN CRIPTOGRÁFICA
                    // Ahora validamos con los datos ya cargados en memoria RAM
                    if (!VerifyPasswordHash(password, (byte[])userData.PasswordHash, (byte[])userData.PasswordSalt))
                    {
                        _logger.LogWarning("⚠️ Credenciales incorrectas para: {User}", username);
                        throw new Exception("Las credenciales son incorrectas.");
                    }

                    // 4. MAPEO DE SESIÓN
                    var userSession = new UserSessionDto
                    {
                        IdUsuario = userData.IdUsuario,
                        Username = userData.Username,
                        Perfil = userData.Perfil,
                        IdSocio = userData.IdSocio,
                        NombreCompleto = userData.NombreCompleto,
                        RequiereCambioPassword = userData.RequiereCambioPassword
                    };

                    // 5. RESPUESTA FINAL
                    return new LoginResponseDto
                    {
                        Usuario = userSession,
                        Menu = menuData,
                        Token = GenerarTokenUnificado(userSession)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 FALLO EN MOTOR AUTH: {Message}", ex.Message);
                throw;
            }
            finally
            {
                _logger.LogInformation("🛰️ Protocolo de autenticación finalizado.");
            }
        }
        // 💎 ÚNICO MOTOR DE TOKENS (Titanium Standard)
        private string GenerarTokenUnificado(UserSessionDto usuario)
        {
            // 🔍 ESCÁNER DE SECCIÓN
            var jwtSection = _config.GetSection("JwtSettings");

            if (!jwtSection.Exists())
                throw new Exception("🚨 EMERGENCIA: La sección 'JwtSettings' no existe en appsettings.json");

            var secretKey = jwtSection["SecretKey"];

            if (string.IsNullOrEmpty(secretKey))
                throw new Exception("🚨 EMERGENCIA: 'SecretKey' está vacío o nulo en JwtSettings.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 💎 INYECCIÓN DE CLAIMS TITANIUM
            // Debemos usar los nombres EXACTOS que sus controladores buscan
            var claims = new List<Claim>
    {
        new Claim("IdUsuario", usuario.IdUsuario.ToString()),
        new Claim(ClaimTypes.Name, usuario.Username),
        
        // 🚀 CRÍTICO: Estas son las llaves que abren el radar de créditos y pagos
        new Claim("Perfil", usuario.Perfil ?? "Socio"),
        new Claim("IdSocio", usuario.IdSocio?.ToString() ?? ""), // Evitamos nulos con ""
        
        // Mantenemos Role para compatibilidad con [Authorize(Roles="...")]
        new Claim(ClaimTypes.Role, usuario.Perfil ?? "Socio")
    };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims), // 🚀 Usamos la lista completa de claims
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSection["DurationInMinutes"] ?? "480")),
                SigningCredentials = creds,
                Issuer = jwtSection["Issuer"],
                Audience = jwtSection["Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        private bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(storedHash);
        }

        public async Task<bool> VerificarPassword(string password, byte[] hash, byte[] salt)
        {
            if (hash == null || salt == null) return false;
            using var hmac = new System.Security.Cryptography.HMACSHA512(salt);
            var hashCalculado = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return await Task.FromResult(hashCalculado.SequenceEqual(hash));
        }

        public async Task<(byte[] hash, byte[] salt)> HashearPassword(string password)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA512();
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            var salt = hmac.Key;
            return await Task.FromResult((hash, salt));
        }
    }
}
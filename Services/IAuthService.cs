using CooperativaApp.DTOS;
using CooperativaApp.Models;

namespace CooperativaApp.Services
{
    public interface IAuthService
    {

        // Genera el Hash y el Salt para guardar en BD
        Task<(byte[] hash, byte[] salt)> HashearPassword(string password);

        // Verifica si la clave ingresada coincide con el Hash de la BD
        Task<bool> VerificarPassword(string password, byte[] hash, byte[] salt);

        // Genera el Token JWT tras un login exitoso
      //  string CrearToken(Usuario usuario);

       Task<LoginResponseDto> Authenticate(string username, string password);

        // 🛡️ Métodos de apoyo para seguridad (Sincronizados con AuthService)
        
        
    }
}

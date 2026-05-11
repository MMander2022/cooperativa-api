using CooperativaApp.Models;

namespace CooperativaApp.Interfaces
{
    public interface IUsuarioRepository
    {
        Task<Usuario?> ObtenerPorUsername(string username);
        Task<Usuario?> ObtenerPorId(int id);
        Task<bool> Actualizar(Usuario usuario);
    }
}

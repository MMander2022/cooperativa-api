using Microsoft.EntityFrameworkCore;
using CooperativaApp.Data;
using CooperativaApp.Interfaces;
using CooperativaApp.Models;

namespace CooperativaApp.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly CooperativaContext _context;
        public UsuarioRepository(CooperativaContext context) => _context = context;

        public async Task<Usuario?> ObtenerPorUsername(string username)
        {
            return await _context.Usuarios
                .Include(u => u.Perfil) // Cargamos el perfil para el Token
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<Usuario?> ObtenerPorId(int id)
        {
            return await _context.Usuarios.FindAsync(id);
        }
        public async Task<bool> Actualizar(Usuario usuario)
        {
            _context.Usuarios.Update(usuario);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}

using CooperativaApp.Data;
using Microsoft.EntityFrameworkCore;
using CooperativaApp.Models;

namespace CooperativaApp.Services
{
    // Services/IAuditoriaService.cs
    public interface IAuditoriaService
    {
        Task RegistrarLog(int? usuarioId, string accion, string detalle, string ip);
    }

    // Implementación escalable
    public class AuditoriaService : IAuditoriaService
    {
        private readonly CooperativaContext _context;
        public AuditoriaService(CooperativaContext context) => _context = context;

        public async Task RegistrarLog(int? usuarioId, string accion, string detalle, string ip)
        {
            var log = new LogActividad
            {
                IdUsuario = usuarioId,
                Accion = accion,
                Detalle = detalle,
                IP = ip,
                FechaRegistro = DateTime.Now
            };
            _context.LogsActividad.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}

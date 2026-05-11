using CooperativaApp.Data;
using CooperativaApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // 🚀 Esta es la clave
namespace CooperativaApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Base protegida
    public class AdminController : ControllerBase
    {
        private readonly CooperativaContext _context;
        public AdminController(CooperativaContext context) => _context = context;

        [HttpGet("permisos/{idPerfil}")]
        [Authorize]
        public async Task<IActionResult> GetPermisosPorPerfil(int idPerfil)
        {
            var asignados = await _context.PerfilModulo
                .Where(pm => pm.IdPerfil == idPerfil)
                .Select(pm => pm.IdModulo)
                .ToListAsync();
            return Ok(asignados);
        }

        [HttpPost("configurar-accesos")]
        public async Task<IActionResult> ConfigurarAccesos([FromBody] RegistroAccesosDTO dto)
        {
            // 🔐 Validación Pro: Solo un Rol Admin puede ejecutar esta escritura
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole?.ToLower() != "admin") return Forbid();

            var antiguos = _context.PerfilModulo.Where(pm => pm.IdPerfil == dto.IdPerfil);
            _context.PerfilModulo.RemoveRange(antiguos);

            foreach (var idModulo in dto.IdsModulos)
            {
                _context.PerfilModulo.Add(new PerfilModulo { IdPerfil = dto.IdPerfil, IdModulo = idModulo });
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Matriz de permisos actualizada con éxito." });
        }
    }
}

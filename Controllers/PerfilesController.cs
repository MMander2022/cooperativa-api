using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CooperativaApp.Data;

namespace CooperativaApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Esto genera la ruta /api/perfiles
    public class PerfilesController : ControllerBase
    {
        private readonly CooperativaContext _context;

        public PerfilesController(CooperativaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetPerfiles()
        {
            var perfiles = await _context.Perfiles
                .Select(p => new { p.IdPerfil, p.Nombre })
                .ToListAsync();
            return Ok(perfiles);
        }
    }
}
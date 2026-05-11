using CooperativaApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace CooperativaApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaestrosController : ControllerBase
    {
        private readonly CooperativaContext _context;
        public MaestrosController(CooperativaContext context) => _context = context;

        [HttpGet("medios-pago")]
        public async Task<IActionResult> GetMediosPago()
        {
            var medios = await _context.MedioPago
                .OrderBy(m => m.Nombre)
                .Select(m => new { m.IdMedioPago, m.Nombre })
                .ToListAsync();
            return Ok(medios);
        }
    }
}

using CooperativaApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using CooperativaApp.Data;

namespace CooperativaApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MoraController : ControllerBase
    {
        private readonly CooperativaContext _context;

        public MoraController(CooperativaContext context)
        {
            _context = context;
        }

        [HttpPost("generar-mora")]
        public async Task<IActionResult> GenerarMora()
        {
            var hoy = DateTime.Today;

            var cuotasVencidas = await _context.Cuotas
                .Where(c => c.FechaVencimiento < hoy && c.Saldo > 0)
                .ToListAsync();

            foreach (var cuota in cuotasVencidas)
            {
                var existeMora = await _context.Moras
                    .AnyAsync(m => m.IdCuota == cuota.IdCuota && m.Estado == "PENDIENTE");

                if (existeMora) continue;

                var dias = (hoy - cuota.FechaVencimiento).Days;
                decimal tasa = 0.01m;
                decimal montoMora = cuota.Saldo * tasa * dias;

                var mora = new Mora
                {
                    IdCuota = cuota.IdCuota,
                    FechaInicio = cuota.FechaVencimiento,
                    DiasMora = dias,
                    MontoMora = montoMora,
                    SaldoMora = montoMora,
                    Estado = "PENDIENTE"
                };

                _context.Moras.Add(mora);
            }

            await _context.SaveChangesAsync();

            return Ok("Moras generadas");
        }
    }
}

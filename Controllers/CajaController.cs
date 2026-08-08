using CooperativaApp.Data;
using CooperativaApp.Models;
using CooperativaApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class CajaController : ControllerBase
{
    private readonly CooperativaContext _context;
    private readonly ICajaService _cajaService;
    public CajaController(CooperativaContext context, ICajaService cajaService)
    {
            _cajaService = cajaService;
            _context = context;
    }

[HttpPost("registrar-movimiento")]
    public async Task<IActionResult> RegistrarMovimiento([FromBody] MovimientoRequest req)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Obtener la configuración del concepto
            var concepto = await _context.ConceptosOperacion.FindAsync(req.IdConcepto);
            if (concepto == null) return BadRequest("Concepto no configurado.");

            // 2. Crear el Asiento Contable primero
            var asiento = new AsientosContables
            {
                Fecha = DateTime.Now,
                Glosa = req.Comentario ?? $"Registro de {concepto.Nombre}",
                Origen = "CAJA"
            };
            _context.AsientosContables.Add(asiento);
            await _context.SaveChangesAsync();

            // 3. Crear Partida Doble (Debe y Haber)
            _context.DetalleAsiento.Add(new DetalleAsiento
            {
                IdAsiento = asiento.IdAsiento,
                CuentaContable = concepto.CuentaContableDebe,
                Debe = req.Monto
            });

            _context.DetalleAsiento.Add(new DetalleAsiento
            {
                IdAsiento = asiento.IdAsiento,
                CuentaContable = concepto.CuentaContableHaber,
                Haber = req.Monto
            });

            // 4. Registrar Movimiento de Caja con el Link al Asiento
            var movimiento = new MovimientoCaja
            {
                IdConcepto = req.IdConcepto,
                IdCredito = req.IdReferencia,
                Monto = req.Monto,
                IdUsuario = req.IdUsuario,
                IdAsiento=asiento.IdAsiento,
                Estado="ACTIVO"
                //IdAsiento = asiento.IdAsiento
            };
            _context.MovimientosCaja.Add(movimiento);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { MovimientoId = movimiento.IdMovimiento, AsientoId = asiento.IdAsiento });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ex.Message);
        }
    }
    [HttpGet("saldo-actual")]
    public async Task<IActionResult> GetSaldoCaja()
    {
        var hoy = DateTime.Today;

        // 🎯 FIX: Se valida m.Fecha.HasValue y m.Fecha.Value.Date para evitar error en DateTime?
        var movimientos = await _context.MovimientosCaja
            .Include(m => m.Concepto)
            .AsNoTracking()
            .Where(m => m.Fecha.HasValue && m.Fecha.Value.Date == hoy)
            .ToListAsync();

        var ingresos = movimientos
            .Where(x => x.Concepto != null && x.Concepto.TipoMovimiento == "I")
            .Sum(x => x.Monto);

        var egresos = movimientos
            .Where(x => x.Concepto != null && x.Concepto.TipoMovimiento == "E")
            .Sum(x => x.Monto);

        return Ok(new
        {
            fecha = hoy,
            ingresos,
            egresos,
            saldoEstimado = ingresos - egresos
        });
    }
    [HttpGet("cuadre-diario")]
    public async Task<IActionResult> GetCuadreDiario([FromQuery] string fecha)
    {
        if (!DateTime.TryParse(fecha, out DateTime fechaParsed))
        {
            return BadRequest(new { message = "Formato de fecha inválido. Usar YYYY-MM-DD." });
        }

        var result = await _cajaService.ObtenerCuadreDiarioAsync(fechaParsed);
        return Ok(result);
    }
}

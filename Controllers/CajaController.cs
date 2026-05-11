using CooperativaApp.Data;
using CooperativaApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class CajaController : ControllerBase
{
    private readonly CooperativaContext _context;

    public CajaController(CooperativaContext context) => _context = context;

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
        // Sumamos Ingresos ('I') y restamos Egresos ('E') usando la relación con Conceptos
        var movimientos = await _context.MovimientosCaja
            .Include(m => m.Concepto)
            .Where(m => m.Fecha.Date == DateTime.Today)
            .ToListAsync();

        var ingresos = movimientos.Where(x => x.Concepto.TipoMovimiento == "I").Sum(x => x.Monto);
        var egresos = movimientos.Where(x => x.Concepto.TipoMovimiento == "E").Sum(x => x.Monto);

        return Ok(new
        {
            fecha = DateTime.Today,
            ingresos,
            egresos,
            saldoEstimado = ingresos - egresos
        });
    }
}

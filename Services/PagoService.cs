using CooperativaApp.Data;
using CooperativaApp.DTOS;
using CooperativaApp.Interfaces;
using CooperativaApp.Models;
using Microsoft.EntityFrameworkCore;
namespace CooperativaApp.Services
{
    public class PagoService : IPagoService
    {
        private readonly CooperativaContext _context;

        public PagoService(CooperativaContext context)
        {
            _context = context;
        }
        public async Task<OperacionResponse> ProcesarPagoAsync(PagoRequestDTO request, int usuarioId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 💎 Truco Diamante: Usamos una clase que coincida con el SELECT del SP
                var resultado = await _context.Set<OperacionResponse>()
                    .FromSqlInterpolated($@"EXEC [dbo].[usp_ProcesarPagoMegaDiamante] 
                @IdCredito={request.IdCredito}, 
                @IdSocio={request.IdSocio},
                @MontoAPagar={request.Monto}, 
                @IdUsuario={usuarioId}, 
                @IdCaja={request.IdCaja}, 
                @ModalidadPago={request.Modalidad}")
                    .ToListAsync();

                var response = resultado.FirstOrDefault();

                if (response != null && response.Exito)
                {
                    await transaction.CommitAsync();
                    return response;
                }

                await transaction.RollbackAsync();
                return response ?? new OperacionResponse { Exito = false, Mensaje = "Fallo de motor" };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new OperacionResponse { Exito = false, Mensaje = ex.Message };
            }
        }
        private async Task ProcesarCuota(DetallePagoRequest item, int idPago)
        {
            var cuota = await _context.Cuotas
                .FirstOrDefaultAsync(x => x.IdCuota == item.IdCuota);

            if (cuota == null)
                throw new Exception($"Cuota {item.IdCuota} no existe");

            decimal monto = item.Monto;

            decimal moraPagada = 0;
            decimal interesPagado = 0;
            decimal capitalPagado = 0;

            // 🔴 1. Pagar Mora
            var mora = await _context.Moras
                .FirstOrDefaultAsync(x => x.IdCuota == cuota.IdCuota && x.Estado == "PENDIENTE");

            if (mora != null && mora.SaldoMora.HasValue && mora.SaldoMora > 0)
            {
                moraPagada = Math.Min(monto, mora.SaldoMora.Value);
                monto -= moraPagada;

                mora.SaldoMora -= moraPagada;
                mora.MontoPagado += moraPagada;

                if (mora.SaldoMora <= 0)
                    mora.Estado = "PAGADO";
            }

            // 🟡 2. Pagar Interés (usar SaldoInteres)
            if (monto > 0 && cuota.SaldoInteres > 0)
            {
                interesPagado = Math.Min(monto, cuota.SaldoInteres);
                monto -= interesPagado;
                cuota.SaldoInteres -= interesPagado;
            }

            // 🟢 3. Pagar Capital (usar SaldoCapital)
            if (monto > 0 && cuota.SaldoCapital > 0)
            {
                capitalPagado = Math.Min(monto, cuota.SaldoCapital);
                monto -= capitalPagado;
                cuota.SaldoCapital -= capitalPagado;
            }

            // 🔵 4. Recalcular saldo total
            cuota.Saldo = cuota.SaldoCapital + cuota.SaldoInteres;

            if (cuota.Saldo <= 0)
                cuota.Estado = "PAGADO";

            // 🔵 4. Actualizar Crédito
            var credito = await _context.Creditos
                .FirstOrDefaultAsync(c => c.IdCredito == cuota.IdCredito);

            if (credito != null)
            {
                credito.SaldoCapital -= capitalPagado;

                if (credito.SaldoCapital <= 0)
                    credito.Estado = "CANCELADO";
            }

            // 🟣 5. Registrar Detalle
            var detalle = new DetallePago
            {
                IdPago = idPago,
                IdCuota = cuota.IdCuota,
                //IdMora = mora?.IdMora,
               // MoraPagada = moraPagada,
                Monto = interesPagado
                // CapitalPagado = capitalPagado
                //Fecha = DateTime.Now
            };

            _context.DetallePago.Add(detalle);
        }

    }
}

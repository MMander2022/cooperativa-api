using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using CooperativaApp.Data;
using CooperativaApp.Models;

namespace CooperativaApp.Jobs
{
    public class MoraJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public MoraJob(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<CooperativaContext>();

                var hoy = DateTime.Today;

                var config = await context.ConfiguracionMora
                    .FirstOrDefaultAsync(c => c.Activo, stoppingToken);

                if (config == null)
                {
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                    continue;
                }

                var cuotasVencidas = await context.Cuotas
                    .Include(c => c.Credito)
                    .Where(c => c.FechaVencimiento < hoy && c.Saldo > 0)
                    .ToListAsync(stoppingToken);

                foreach (var cuota in cuotasVencidas)
                {
                    int diasMora = (hoy - cuota.FechaVencimiento).Days - config.DiasGracia;

                    if (diasMora <= 0)
                        continue;

                    decimal montoMora = CalcularMora(cuota.Saldo, diasMora, config);

                    var mora = await context.Moras
                        .FirstOrDefaultAsync(m => m.IdCuota == cuota.IdCuota, stoppingToken);

                    if (mora == null)
                    {
                        mora = new Mora
                        {
                            IdCuota = cuota.IdCuota,
                            FechaGeneracion = DateTime.Now,
                            FechaInicio = cuota.FechaVencimiento,
                            DiasMora = diasMora,
                            MontoMora = montoMora,
                            SaldoMora = montoMora,
                            Estado = "PENDIENTE"
                        };

                        context.Moras.Add(mora);
                    }
                    else
                    {
                        decimal pagado = mora.MontoPagado ?? 0;

                        mora.DiasMora = diasMora;
                        mora.MontoMora = montoMora;
                        mora.SaldoMora = montoMora - pagado;

                        mora.Estado = mora.SaldoMora <= 0 ? "PAGADO" : "PENDIENTE";
                    }

                    cuota.Estado = "MOROSO";

                    AplicarFinancieramente(context, cuota, montoMora, config);

                    //GenerarAsientoContable(context, cuota.IdCuota, montoMora);
                }

                await context.SaveChangesAsync(stoppingToken);

                // ⏳ Ejecutar cada 24 horas (usar minutos para pruebas)
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        // ===========================
        // CÁLCULO DE MORA
        // ===========================
        private decimal CalcularMora(decimal saldo, int dias, ConfiguracionMora config)
        {
            return config.Tipo switch
            {
                "DIARIA_SIMPLE" => saldo * config.Tasa * dias,

                "DIARIA_COMPUESTA" =>
                    saldo * (decimal)Math.Pow((double)(1 + config.Tasa), dias) - saldo,

                "FIJA" => (config.MontoFijo ?? 0) * dias,

                "MIXTA" =>
                    ((config.MontoFijo ?? 0) * dias) + (saldo * config.Tasa * dias),

                "MENSUAL" =>
                    saldo * config.Tasa * (dias / 30),

                _ => 0
            };
        }

        // ===========================
        // APLICACIÓN FINANCIERA
        // ===========================
        private void AplicarFinancieramente(
            CooperativaContext context,
            Cuota cuota,
            decimal montoMora,
            ConfiguracionMora config)
        {
            switch (config.TipoAplicacion)
            {
                case "CAPITALIZAR":
                    var credito = cuota.Credito;
                    if (credito != null)
                    {
                        credito.SaldoCapital += montoMora;
                    }
                    break;

                case "REFINANCIAR":
                    var nuevoCredito = new Credito
                    {
                        IdSocio = cuota.Credito.IdSocio,
                        Monto = montoMora,
                        SaldoCapital = montoMora,
                        Estado = "VIGENTE",
                        FechaSolicitud = DateTime.Now,
                        FechaAprobacion = DateTime.Now
                    };
                    context.Creditos.Add(nuevoCredito);
                    break;

                case "INDEPENDIENTE":
                default:
                    // No afecta crédito
                    break;
            }
        }

        // ===========================
        // ASIENTO CONTABLE AUTOMÁTICO
        // ===========================
       /* private void GenerarAsientoContable(
            CooperativaContext context,
            int idCuota,
            decimal monto)
        {
            var asiento = new AsientosContables
            {
                Fecha = DateTime.Now,
                TipoOperacion = "MORA_GENERADA",
                ReferenciaId = idCuota,
                CuentaDebe = "CUENTAS_POR_COBRAR_MORA",
                CuentaHaber = "INGRESO_MORA",
                Monto = monto,
                Estado = "GENERADO"
            };

            context.AsientosContables.Add(asiento);
        }*/
    }
}

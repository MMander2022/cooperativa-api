using CooperativaApp.Data;
using CooperativaApp.DTOs;
using CooperativaApp.DTOS;
using CooperativaApp.Utils;
using CooperativaApp.Interfaces;
using CooperativaApp.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using static CooperativaApp.Controllers.CreditosController;

namespace CooperativaApp.Services
{
    public class CreditoService : ICreditoService
    {
        private readonly CooperativaContext _context;
        private readonly ILogger<CreditoService> _logger;
        public CreditoService(CooperativaContext context, ILogger<CreditoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> DesembolsarCreditoAsync(int idCredito, int idUsuario)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var credito = await _context.Creditos.FindAsync(idCredito)
                    ?? throw new Exception("Crédito no encontrado");

                if (credito.Estado != "APROBADO") throw new Exception("Solo se pueden desembolsar créditos aprobados");

                // 1. Cambiar estado
                credito.Estado = "DESEMBOLSADO";
                credito.FechaDesembolso = DateTimeUtils.ObtenerHoraPeru();
                
                // 2. Buscar Concepto Maestro para Contabilidad
                var concepto = await _context.ConceptosOperacion
                    .FirstOrDefaultAsync(c => c.Nombre.ToUpper().Contains("DESEMBOLSO"))
                    ?? throw new Exception("Concepto 'DESEMBOLSO' no configurado en la tabla maestra");

                // 3. Crear Asiento Contable (Partida Doble)
                var asiento = new AsientosContables
                {
                    Fecha = DateTimeUtils.ObtenerHoraPeru(),
                    Glosa = $"Desembolso Crédito #{idCredito} - Socio: {credito.IdSocio}",
                    Origen = "DESEMBOLSO",
                    IdReferencia = idCredito
                };
                _context.AsientosContables.Add(asiento);
                await _context.SaveChangesAsync();

                // Debe: Cartera de Créditos (Activo aumenta) / Haber: Caja (Activo disminuye)
                _context.DetalleAsiento.Add(new DetalleAsiento { IdAsiento = asiento.IdAsiento, CuentaContable = concepto.CuentaContableDebe, Debe = credito.Monto });
                _context.DetalleAsiento.Add(new DetalleAsiento { IdAsiento = asiento.IdAsiento, CuentaContable = concepto.CuentaContableHaber, Haber = credito.Monto });

                // 4. Registrar Movimiento de Caja (EGRESO)
                _context.MovimientosCaja.Add(new MovimientoCaja
                {
                    IdConcepto = concepto.IdConcepto,
                    IdCredito = idCredito,
                    Monto = credito.Monto,
                    IdUsuario = idUsuario,
                    IdAsiento= asiento.IdAsiento,
                    Estado="ACTIVO"
                    // Nota: El 'Tipo' se saca por el Concepto ('E')
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return "DES-" + idCredito.ToString("D6");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // Extraemos el error profundo
                var realError = ex.InnerException?.InnerException?.Message
                                ?? ex.InnerException?.Message
                                ?? ex.Message;

                // Esto lo verás en la consola de Visual Studio (ventana Output)
                Console.WriteLine($"--- ERROR EN DB: {realError} ---");

                // Lanzamos una nueva excepción con el detalle real
                throw new Exception(realError);
            }
        }

        public async Task GenerarCuotasAsync(int idCredito)
        {
            var credito = await _context.Creditos
                .FirstOrDefaultAsync(x => x.IdCredito == idCredito);

            if (credito == null)
                throw new Exception("Crédito no encontrado");

            // 🔹 Eliminar cuotas anteriores
            var cuotas = _context.Cuotas
                .Where(x => x.IdCredito == idCredito);

            _context.Cuotas.RemoveRange(cuotas);
            await _context.SaveChangesAsync();

            credito.SaldoCapital = credito.Monto;

            var tipo = credito.TipoCalculo?.Trim().ToUpper();

            switch (tipo)
            {
                case "FRANCES":
                    GenerarFrances(credito);
                    break;

                case "ALEMAN":
                    GenerarAleman(credito);
                    break;

                case "INTERES_SIMPLE":
                    GenerarInteresSimple(credito);
                    break;

                default:
                    GenerarFrances(credito);
                    break;
            }

            await _context.SaveChangesAsync();
        }

        private void GenerarFrances(Credito credito)
        {
            decimal P = credito.Monto;
            // Asumimos Tasa Mensual. Si es Anual, dividir entre 12.
            decimal i = (credito.TasaInteres / 100);
            int n = credito.PlazoMeses;

            // Fórmula de Cuota Constante (Método Francés)
            // Usamos double solo para la potencia, pero volvemos a decimal inmediatamente
            double factor = Math.Pow(1 + (double)i, n);
            decimal cuotaFija = P * (i * (decimal)factor) / ((decimal)factor - 1);
            cuotaFija = Math.Round(cuotaFija, 2); // Redondeo financiero estándar

            decimal saldoRestante = P;

            for (int k = 1; k <= n; k++)
            {
                decimal interes = Math.Round(saldoRestante * i, 2);
                decimal capital = cuotaFija - interes;

                // Ajuste en la última cuota para evitar céntimos huérfanos por redondeo
                if (k == n)
                {
                    capital = saldoRestante;
                    cuotaFija = capital + interes;
                    saldoRestante = 0;
                }
                else
                {
                    saldoRestante -= capital;
                }

                _context.Cuotas.Add(new Cuota
                {
                    IdCredito = credito.IdCredito,
                    NumeroCuota = k,
                    FechaVencimiento = DateTime.Today.AddMonths(k), // Evitar DateTime.Now para vencimientos fijos
                    Capital = capital,
                    Interes = interes,
                    MontoCuota = cuotaFija,
                    SaldoCapital = saldoRestante, // El saldo que queda después de esta cuota
                    Estado = "PENDIENTE",
                    // Los campos SaldoInteres y Saldo suelen usarse para pagos parciales
                    Saldo = cuotaFija
                });
            }
        }
        private void GenerarAleman(Credito credito)
        {
            decimal capitalFijo = credito.Monto / credito.PlazoMeses;
            decimal saldo = credito.Monto;
            decimal i = credito.TasaInteres / 100;

            for (int k = 1; k <= credito.PlazoMeses; k++)
            {
                decimal interes = saldo * i;
                decimal cuota = capitalFijo + interes;

                saldo -= capitalFijo;

                _context.Cuotas.Add(new Cuota
                {
                    IdCredito = credito.IdCredito,
                    NumeroCuota = k,
                    FechaVencimiento = DateTime.Now.AddMonths(k),
                    Capital = capitalFijo,
                    Interes = interes,
                    SaldoCapital = capitalFijo,
                    SaldoInteres = interes,
                    MontoCuota = cuota,
                    Saldo = cuota,
                    Estado = "PENDIENTE"
                });
            }
        }

        private void GenerarInteresSimple(Credito credito)
        {
            decimal i = credito.TasaInteres / 100;

            decimal interesTotal =
                credito.Monto * i * credito.PlazoMeses;

            decimal cuota =
                (credito.Monto + interesTotal) / credito.PlazoMeses;

            decimal capitalMensual =
                credito.Monto / credito.PlazoMeses;

            decimal interesMensual =
                interesTotal / credito.PlazoMeses;

            for (int k = 1; k <= credito.PlazoMeses; k++)
            {
                _context.Cuotas.Add(new Cuota
                {
                    IdCredito = credito.IdCredito,
                    NumeroCuota = k,
                    FechaVencimiento = DateTime.Now.AddMonths(k),
                    Capital = capitalMensual,
                    Interes = interesMensual,
                    SaldoCapital = capitalMensual,
                    SaldoInteres = interesMensual,
                    MontoCuota = cuota,
                    Saldo = cuota,
                    Estado = "PENDIENTE"
                });
            }
        }
        //public async Task<SimulacionResponseDTO> SimularCreditoAsync(SimulacionRequestDTO request)
        //{
        //    // 1. Obtener el producto e incluir Tasas
        //    var producto = await _context.Productos
        //        .Include(p => p.Tasas)
        //        .FirstOrDefaultAsync(p => p.Id == request.ProductoId);

        //    if (producto == null) throw new Exception("Producto no encontrado");

        //    // 2. Determinar la tasa aplicada
        //    var tasaAnual = producto.Tasas
        //        .Where(t => request.Monto >= t.MontoMinimo && request.Monto <= t.MontoMaximo)
        //        .Select(t => t.TasaInteres)
        //        .FirstOrDefault();

        //    if (tasaAnual == 0) tasaAnual = producto.TasaReferencial ?? 0;

        //    // 3. Lógica matemática (Sistema Francés)
        //    decimal P = request.Monto;
        //    decimal i = (tasaAnual / 100) / 12; // Tasa mensual
        //    int n = request.PlazoMeses;

        //    double factor = Math.Pow(1 + (double)i, n);
        //    decimal cuotaFija = P * (i * (decimal)factor) / ((decimal)factor - 1);
        //    cuotaFija = Math.Round(cuotaFija, 2);

        //    var cronograma = new List<CuotaDetalleDTO>();
        //    decimal saldoRestante = P;

        //    for (int k = 1; k <= n; k++)
        //    {
        //        decimal interes = Math.Round(saldoRestante * i, 2);
        //        decimal capital = (k == n) ? saldoRestante : Math.Round(cuotaFija - interes, 2);
        //        decimal montoCuota = Math.Round(capital + interes, 2);

        //        saldoRestante -= capital;
        //        decimal saldoFinalCuota = Math.Max(0, Math.Round(saldoRestante, 2));

        //        // 💎 INYECCIÓN AL CONSTRUCTOR DIAMANTE REFACTORIZADO
        //        cronograma.Add(new CuotaDetalleDTO(
        //            k,                                  // numeroCuota
        //            DateTime.Today.AddMonths(k),        // fechaVencimiento
        //            montoCuota,                         // montoCuota
        //            "SIMULADO",                         // estado
        //            montoCuota,                         // saldoCuota (En simulación, el saldo es la cuota entera)
        //            saldoFinalCuota,                    // saldoCapital (Proyectado)
        //            null,                               // fechaPago (Null en simulación)
        //            0,                                  // montoPagadoReal
        //            0,                                  // montoEnRevision
        //            null                                // medioRevision
        //        ));
        //    }

        //    return new SimulacionResponseDTO(producto.Nombre, tasaAnual, cuotaFija, cronograma);
        //}
        public async Task<SimulacionResponseDTO> SimularCreditoAsync(SimulacionRequestDTO request)
        {
            // 1. Obtener el producto e incluir Tasas
            var producto = await _context.Productos
                .Include(p => p.Tasas)
                .FirstOrDefaultAsync(p => p.Id == request.ProductoId);

            if (producto == null) throw new Exception("Producto no encontrado");

            // 2. Determinar la tasa aplicada
            var tasaAnual = producto.Tasas
                .Where(t => request.Monto >= t.MontoMinimo && request.Monto <= t.MontoMaximo)
                .Select(t => t.TasaInteres)
                .FirstOrDefault();

            if (tasaAnual == 0) tasaAnual = producto.TasaReferencial ?? 0;

            // 3. Lógica matemática (Sistema Francés)
            decimal P = request.Monto;
            decimal i = (tasaAnual / 100) / 12; // Tasa mensual
            int n = request.PlazoMeses;

            double factor = Math.Pow(1 + (double)i, n);
            decimal cuotaFija = P * (i * (decimal)factor) / ((decimal)factor - 1);
            cuotaFija = Math.Round(cuotaFija, 2);

            var cronograma = new List<CuotaDetalleDTO>();
            decimal saldoRestante = P;

            for (int k = 1; k <= n; k++)
            {
                decimal interes = Math.Round(saldoRestante * i, 2);
                decimal capital = (k == n) ? saldoRestante : Math.Round(cuotaFija - interes, 2);
                decimal montoCuota = Math.Round(capital + interes, 2);

                saldoRestante -= capital;
                decimal saldoFinalProyectado = Math.Max(0, Math.Round(saldoRestante, 2));

                // 💎 INICIALIZACIÓN DIRECTA (Compatible con DTO sin constructor)
                cronograma.Add(new CuotaDetalleDTO
                {
                    NumeroCuota = k,
                    FechaVencimiento = DateTime.Today.AddMonths(k),
                    MontoCuota = montoCuota,
                    Estado = "SIMULADO",
                    SaldoCuota = montoCuota, // En simulación la deuda es la cuota completa
                    SaldoCapital = saldoFinalProyectado,
                    FechaPago = null,
                    MontoPagadoReal = 0,
                    MontoEnRevision = 0,
                    MedioRevision = null,
                    MedioPagoReal = null,
                    FechaSolicitud = null
                });
            }

            return new SimulacionResponseDTO(producto.Nombre, tasaAnual, cuotaFija, cronograma);
        }

        public async Task<EstadoCuentaResponse> ObtenerEstadoCuentaAsync(int idCredito)
        {
            var credito = await _context.Creditos
             .FirstOrDefaultAsync(c => c.IdCredito == idCredito);
            var cuotas = await _context.Cuotas
            .Where(c => c.IdCredito == idCredito)
            .ToListAsync();

            if (credito == null)
                throw new Exception("Crédito no encontrado");

            var detalles = await (
            from d in _context.DetallePago
            join c in _context.Cuotas
            on d.IdCuota equals c.IdCuota
            where c.IdCredito == idCredito
            select d
            ).ToListAsync();

            var moras = await (
                    from m in _context.Moras
                    join c in _context.Cuotas
                        on m.IdCuota equals c.IdCuota
                    where c.IdCredito == idCredito
                    select m
                ).ToListAsync();

            decimal totalCapitalPagado = detalles.Sum(x => x.CapitalPagado);
            decimal totalInteresPagado = detalles.Sum(x => x.InteresPagado);
           // decimal totalMoraPagada = detalles.Sum(x => x.MoraPagada);

            decimal moraPendiente = moras
                .Where(x => x.Estado == "PENDIENTE")
                .Sum(x => x.SaldoMora ?? 0);

            int cuotasPagadas = _context.Cuotas.Count(x => x.Estado == "PAGADO");
            int cuotasPendientes = _context.Cuotas.Count(x => x.Estado == "PENDIENTE");

            int cuotasVencidas = _context.Cuotas.Count(x => x.Estado == "PENDIENTE" && x.FechaVencimiento < DateTimeUtils.ObtenerHoraPeru());

            decimal totalPendiente = _context.Cuotas
                .Where(x => x.Estado == "PENDIENTE")
                .Sum(x => x.Saldo);

            return new EstadoCuentaResponse
            {
                IdCredito = credito.IdCredito,
                MontoOriginal = credito.Monto,
                SaldoCapitalActual = credito.SaldoCapital,

                TotalCapitalPagado = totalCapitalPagado,
                TotalInteresPagado = totalInteresPagado,
              //  TotalMoraPagada = totalMoraPagada,

                TotalPendiente = totalPendiente + moraPendiente,

                CuotasPagadas = cuotasPagadas,
                CuotasPendientes = cuotasPendientes,
                CuotasVencidas = cuotasVencidas,

                MoraAcumulada = moraPendiente,

                EstadoCredito = credito.Estado
            };
        }
        public async Task<List<CronogramaCuotaDTO>> ObtenerCronogramaAsync(int idCredito)
        {
            var cuotas = await _context.Cuotas
                .Where(c => c.IdCredito == idCredito)
                .OrderBy(c => c.NumeroCuota)
                .ToListAsync();

            return cuotas.Select(c => new CronogramaCuotaDTO
            {
                NumeroCuota = c.NumeroCuota,
                FechaVencimiento = c.FechaVencimiento,

                CapitalProgramado = c.Capital,
                InteresProgramado = c.Interes,
                CuotaProgramada = c.MontoCuota,

                SaldoCapitalPendiente = c.SaldoCapital,
                SaldoInteresPendiente = c.SaldoInteres,
                SaldoCuotaPendiente = c.Saldo,

                Estado = c.Estado
            }).ToList();
        }
        
        public async Task<List<KardexDTO>> ObtenerKardexAsync(int idCredito)
        {
            var credito = await _context.Creditos
                .FirstOrDefaultAsync(x => x.IdCredito == idCredito);

            if (credito == null)
                throw new Exception("Crédito no existe");

            decimal saldo = credito.Monto;

            var kardex = new List<KardexDTO>();

            // 1️⃣ DESEMBOLSO
            kardex.Add(new KardexDTO
            {
                Fecha = credito.FechaSolicitud,
                TipoOperacion = "DESEMBOLSO",
                Debe = credito.Monto,
                Haber = 0,
                SaldoCapital = saldo
            });

            // 2️⃣ DEVENGO DE CUOTAS
            var cuotas = await _context.Cuotas
                .Where(c => c.IdCredito == idCredito)
                .OrderBy(c => c.NumeroCuota)
                .ToListAsync();

            foreach (var c in cuotas)
            {
                // INTERES (aumenta deuda)
                saldo += c.Interes;

                kardex.Add(new KardexDTO
                {
                    Fecha = c.FechaVencimiento,
                    TipoOperacion = $"INTERES CUOTA {c.NumeroCuota}",
                    Debe = c.Interes,
                    Haber = 0,
                    SaldoCapital = saldo
                });

                // CAPITAL (reduce deuda programada)
                saldo -= c.Capital;

                kardex.Add(new KardexDTO
                {
                    Fecha = c.FechaVencimiento,
                    TipoOperacion = $"AMORTIZACION CUOTA {c.NumeroCuota}",
                    Debe = 0,
                    Haber = c.Capital,
                    SaldoCapital = saldo
                });
            }
            // 3️⃣ MORA GENERADA
            var moras = await (
                from m in _context.Moras
                join c in _context.Cuotas on m.IdCuota equals c.IdCuota
                where c.IdCredito == idCredito
                orderby m.FechaGeneracion
                select m
            ).ToListAsync();

            foreach (var m in moras)
            {
                saldo += m.MontoMora;

                kardex.Add(new KardexDTO
                {
                    Fecha = m.FechaGeneracion,
                    TipoOperacion = "MORA GENERADA",
                    Debe = m.MontoMora,
                    Haber = 0,
                    SaldoCapital = saldo
                });
            }

            // 3️⃣ PAGOS REALES
            var pagos = await _context.DetallePago
                .Include(d => d.Cuota)
                .Where(d => d.Cuota!.IdCredito == idCredito)
              //  .OrderBy(d => d.Fecha)
                .ToListAsync();

            foreach (var p in pagos)
            {
                saldo -= p.CapitalPagado;

                kardex.Add(new KardexDTO
                {
                   // Fecha = p.Fecha,
                    TipoOperacion = $"PAGO CUOTA {p.Cuota!.NumeroCuota}",
                    Debe = 0,
                    Haber = p.CapitalPagado,
                    SaldoCapital = saldo
                });
            }

            return kardex.OrderBy(x => x.Fecha).ToList();
        }
        public async Task<IEnumerable<object>> ObtenerCreditosPorSocioAsync(int socioId)
        {
            // 🛰️ 1. RADAR DE NÚCLEO FAMILIAR (Protocolo de Integridad Unificado)
            var idsFamilia = await _context.Familiaridad
                .Where(f => f.IdSocioTitular == socioId && f.Activo)
                .Select(f => f.IdSocioFamiliar)
                .ToListAsync();

            // Auto-inyección obligatoria del propio ID del socio titular
            idsFamilia.Add(socioId);

            // 🛰️ 2. EXTRACCIÓN Y MAPEO TITANIUM MULTI-FAMILIAR
            return await _context.Creditos
                .Include(c => c.Socio) // Incluimos la entidad Socio para jalar la trazabilidad de nombres
                .Where(c => idsFamilia.Contains(c.IdSocio))
                .OrderByDescending(c => c.FechaSolicitud)
                .Select(c => new {
                    c.IdCredito,
                    c.Monto,
                    c.Estado,
                    c.FechaSolicitud,
                    c.TasaInteres,
                    c.PlazoMeses,
                    c.MontoDesembolsado,
                    c.FechaDesembolso,
                    c.FechaAprobacion,
                    c.IdSocio,
                    // 🎯 Propiedad crítica mapeada en mayúsculas/minúsculas lista para el front
                    NombreSocio = c.Socio != null ? (c.Socio.Nombres + " " + c.Socio.Apellidos).ToUpper() : "SOCIO NÚCLEO"
                })
                .ToListAsync();
        }
        public async Task<IEnumerable<CuotaDetalleDTO>> ObtenerPlanPagosAsync(int idCredito)
        {
            return await _context.Cuotas
                .Where(q => q.IdCredito == idCredito)
                .OrderBy(q => q.NumeroCuota)
                .Select(q => new CuotaDetalleDTO
                {
                    NumeroCuota = q.NumeroCuota,
                    FechaVencimiento = q.FechaVencimiento,
                    MontoCuota = q.MontoCuota,
                    Estado = q.Estado,
                    FechaPago = q.FechaVencimiento,
                    SaldoCapital=q.SaldoCapital
                }).ToListAsync();
        }
        public async Task<OperacionResponse> RegistrarDesembolsoAsync(DesembolsoRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Fecha de desembolso capturada de pantalla (o Hoy por defecto)
                DateTime fechaDesembolsoReal = request.FechaPrimerDesembolso ?? DateTime.Today;

                // 2. Ejecutar SP Maestro
                var resultado = await _context.Set<OperacionResponse>()
                    .FromSqlInterpolated($@"EXEC [dbo].[usp_RegistrarDesembolso]
                @IdCredito={request.IdCredito},
                @MontoADesembolsar={request.Monto},
                @UsuarioId={request.UsuarioId},
                @IdCaja={request.IdCaja},
                @Observacion={request.Observacion},
                @idMediopago={request.IdMedioPago}")
                    .ToListAsync();

                var response = resultado.FirstOrDefault();

                if (response == null || !response.Exito)
                {
                    await transaction.RollbackAsync();
                    return response ?? new OperacionResponse(false, "Error desconocido en SP Maestro");
                }

                // 3. Consultar entidad Crédito para obtener Tasa y Estado
                var credito = await _context.Creditos
                    .FirstOrDefaultAsync(x => x.IdCredito == request.IdCredito);

                if (credito == null)
                {
                    await transaction.RollbackAsync();
                    return new OperacionResponse(false, "No se encontró la información del crédito.");
                }

                bool esDesembolsoTotal = (credito.Estado?.ToUpper() == "DESEMBOLSADO" || credito.EstadoCredito?.ToUpper() == "DESEMBOLSADO");

                // 🎯 SCENARIO A: DESEMBOLSO PARCIAL -> GENERAR/ACTUALIZAR CUOTA 0 CON INTERÉS SIMPLE REAL
                if (!esDesembolsoTotal)
                {
                    decimal tasaMensual = (credito.TasaInteres > 0 ? credito.TasaInteres : 0m) / 100m;
                    decimal interesCuotaCero = Math.Round(request.Monto * tasaMensual, 2); // Ej: 3000 * 5% = S/ 150.00

                    // Fecha vencimiento Cuota 0 = Fecha Desembolso + 1 Mes
                    DateTime fechaVencCuotaCero = fechaDesembolsoReal.AddMonths(1);

                    // Búsqueda o Creación de la Cuota 0
                    var cuotaCero = await _context.Cuotas
                        .FirstOrDefaultAsync(q => q.IdCredito == request.IdCredito && q.NumeroCuota == 0);

                    if (cuotaCero == null)
                    {
                        cuotaCero = new Cuota
                        {
                            IdCredito = request.IdCredito,
                            NumeroCuota = 0,
                            FechaVencimiento = fechaVencCuotaCero,
                            Capital = 0.00m,
                            Interes = interesCuotaCero,
                            MontoCuota = interesCuotaCero,
                            Saldo = interesCuotaCero,
                            SaldoCapital = 0.00m,
                            SaldoInteres = interesCuotaCero,
                            SaldoMora = 0.00m,
                            MoraGenerada = 0.00m,
                            Estado = "PENDIENTE",
                            EsPrecancelacion = false
                        };
                        _context.Cuotas.Add(cuotaCero);
                    }
                    else
                    {
                        cuotaCero.FechaVencimiento = fechaVencCuotaCero;
                        cuotaCero.Interes = interesCuotaCero;
                        cuotaCero.MontoCuota = interesCuotaCero;
                        cuotaCero.Saldo = interesCuotaCero;
                        cuotaCero.SaldoInteres = interesCuotaCero;
                        cuotaCero.Estado = "PENDIENTE";
                    }

                    // 🎯 DESPLAZAR CUOTAS REGULARES (1..N) A PARTIR DE LA FECHA DE LA CUOTA 0 MANTENIENDO EL DÍA FIJO
                    var cuotasRegulares = await _context.Cuotas
                        .Where(q => q.IdCredito == request.IdCredito && q.NumeroCuota > 0)
                        .OrderBy(q => q.NumeroCuota)
                        .ToListAsync();

                    if (cuotasRegulares.Any())
                    {
                        int diaFijo = cuotasRegulares.First().FechaVencimiento.Day;
                        DateTime fechaBase = fechaVencCuotaCero.AddMonths(1);

                        foreach (var q in cuotasRegulares)
                        {
                            int maxDiasMes = DateTime.DaysInMonth(fechaBase.Year, fechaBase.Month);
                            int diaAjustado = Math.Min(diaFijo, maxDiasMes);
                            q.FechaVencimiento = new DateTime(fechaBase.Year, fechaBase.Month, diaAjustado);
                            fechaBase = fechaBase.AddMonths(1);
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                // 🎯 SCENARIO B: DESEMBOLSO TOTAL / COMPLETADO -> ELIMINAR CUOTA 0 Y RECALIBRAR DESDE CUOTA 1
                else
                {
                    // Si existía una Cuota 0 pendiente, se remueve
                    var cuotaCeroExistente = await _context.Cuotas
                        .FirstOrDefaultAsync(q => q.IdCredito == request.IdCredito && q.NumeroCuota == 0);

                    if (cuotaCeroExistente != null)
                    {
                        int diasParaVencimiento = (cuotaCeroExistente.FechaVencimiento.Date - fechaDesembolsoReal.Date).Days;

                        // 🛡️ REGLA DE 5 DÍAS:
                        // Si el desembolso final se hace con más de 5 días de anticipación a su vencimiento (ej. diasParaVencimiento >= 5),
                        // la Cuota 0 SE ELIMINA. Si ya está dentro de los 5 días previos o vencida, SE CONSERVA.
                        if (diasParaVencimiento >= 5 && (cuotaCeroExistente.Estado ?? "").ToUpper() == "PENDIENTE")
                        {
                            _context.Cuotas.Remove(cuotaCeroExistente);
                        }
                    }

                    // Recalibración del cronograma normal (1..N) con la FechaPrimerDesembolso
                    var cuotasNormales = await _context.Cuotas
                        .Where(q => q.IdCredito == request.IdCredito && q.NumeroCuota > 0)
                        .OrderBy(q => q.NumeroCuota)
                        .ToListAsync();

                    if (cuotasNormales.Any())
                    {
                        int diaFijo = cuotasNormales.First().FechaVencimiento.Day;
                        DateTime fechaIteracion = fechaDesembolsoReal.AddMonths(1);

                        foreach (var q in cuotasNormales)
                        {
                            int maxDiasMes = DateTime.DaysInMonth(fechaIteracion.Year, fechaIteracion.Month);
                            int diaAjustado = Math.Min(diaFijo, maxDiasMes);
                            q.FechaVencimiento = new DateTime(fechaIteracion.Year, fechaIteracion.Month, diaAjustado);
                            fechaIteracion = fechaIteracion.AddMonths(1);
                        }
                    }

                    await _context.SaveChangesAsync();

                    // SP Finalizar Cronograma
                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC [dbo].[usp_FinalizarCronograma] @p0",
                        parameters: new object[] { request.IdCredito }
                    );
                }

                await transaction.CommitAsync();
                return response;
            }
            catch (Exception ex)
            {
                if (_context.Database.CurrentTransaction != null)
                    await transaction.RollbackAsync();

                return new OperacionResponse(false, $"Error en Desembolso: {ex.Message}");
            }
        }
        public async Task<IEnumerable<CreditoSocioDTO>> ObtenerCreditosPorPerfilAsync(int usuarioId, string perfil, int? socioId)
        {
            // 🕵️ DEBUG: Imprimir en la consola del servidor para ver qué llega
            

            // 1. Radar Base: Ampliamos los estados por si acaso
            var query = _context.Creditos
                .Include(c => c.Socio)
                .Where(c => new[] { "DESEMBOLSADO",  "PARCIAL" }.Contains(c.Estado.ToUpper()))
                .AsQueryable();

            // 2. 🛡️ Lógica de Seguridad Diamante (Normalizada)
            // Usamos Equals con OrdinalIgnoreCase para que "admin", "Admin" y "ADMIN" funcionen
            bool esAdmin = perfil.Equals("Administrador", StringComparison.OrdinalIgnoreCase) ||
                           perfil.Equals("Admin", StringComparison.OrdinalIgnoreCase);

            if (!esAdmin)
            {
                // Si NO es admin, el filtro es obligatorio
                if (socioId.HasValue && socioId.Value > 0)
                {
                    query = query.Where(c => c.IdSocio == socioId.Value);
                }
                else
                {
                    // Si el socio no tiene ID vinculado, por seguridad no ve nada
                    return new List<CreditoSocioDTO>();
                }
            }

            // 3. Ejecución de la consulta
            var resultado = await query
                .OrderByDescending(c => c.IdCredito)
                .Select(c => new CreditoSocioDTO
                {
                    IdCredito = c.IdCredito,
                    MontoOriginal = c.Monto,
                    NombreSocio = c.Socio != null ? c.Socio.Nombres + " " + c.Socio.Apellidos : "Socio Externo",
                    Estado = c.Estado,
                    FechaUltimoDesembolso=c.FechaUltimoDesembolso,
                    // 💎 ProximoVencimiento: Si falla aquí, la consulta falla. Aseguramos el LEFT JOIN
                    ProximoVencimiento = _context.Cuotas
                        .Where(q => q.IdCredito == c.IdCredito && q.Estado != "PAGADO")
                        .OrderBy(q => q.NumeroCuota)
                        .Select(q => (DateTime?)q.FechaVencimiento)
                        .FirstOrDefault()
                }).ToListAsync();

            return resultado;
        }
        public async Task<List<CuotaDetalleDTO>> GetPlanPagosConAuditoriaAsync(int idCredito)
        {
            // 1. Ejecución del SP
            var resultado = await _context.Set<CuotaDetalleDTO>()
                .FromSqlRaw("EXEC sp_GetPlanPagosDetalladoNuevo @IdCredito = {0}", idCredito)
                .ToListAsync();

            // 🕵️ SONDA DE AUDITORÍA DIAMANTE (Mira esto en la consola de Visual Studio / Debug)
            Console.WriteLine($"--- AUDITORÍA RADAR CREDITO {idCredito} ---");
            foreach (var q in resultado)
            {
                Console.WriteLine($"Cuota: {q.NumeroCuota} | Estado: {q.Estado} | Real: {q.MontoPagadoReal} | Saldo: {q.SaldoCuota} | Medio: {q.MedioPagoReal}");
            }
            Console.WriteLine("------------------------------------------");

            // 2. Lógica de Negocio y Normalización Táctica
            return resultado.Select(q => {
                // Parche de Emergencia: Si el SP dice PAGADO pero el mapeo falló (llegó 0)
                // forzamos los datos para que el Socio no vea información vacía.
                if (q.Estado == "PAGADO")
                {
                    if (q.MontoPagadoReal == 0) q.MontoPagadoReal = q.MontoCuota;
                    if (q.SaldoCuota != 0) q.SaldoCuota = 0;
                    if (string.IsNullOrEmpty(q.MedioPagoReal)) q.MedioPagoReal = "SISTEMA";

                    q.MontoEnRevision = 0;
                    q.FechaSolicitud = null;
                    q.MedioRevision = null;
                }

                // Si es PENDIENTE y el saldo llegó en 0 por error de mapeo, lo restauramos
                if ((q.Estado == "PENDIENTE" || q.Estado == "MOROSO") && q.SaldoCuota == 0)
                {
                    q.SaldoCuota = q.MontoCuota;
                }

                return q;
            }).ToList();
        }

        public async Task<List<CuotaAnaliticaDTO>> GetPlanPagosAnaliticoAsync(int idCredito)
        {
            try
            {
                // 🎯 Invocación directa al SP de Auditoría Analítica
                var cronograma = await _context.Set<CuotaAnaliticaDTO>()
                    .FromSqlInterpolated($"EXEC sp_GetPlanPagosAnaliticoNuevo @IdCredito = {idCredito}")
                    .ToListAsync();

                // 🕵️ SONDA DE VERIFICACIÓN (Opcional para Debug)
                if (cronograma.Any())
                {
                    var test = cronograma.First();
                    Console.WriteLine($"[BACKEND INFO] Folio: {idCredito} | Cuota 1 Pago Cap: {test.PagoCapital} | Saldo Mora: {test.SaldoMora}");
                }

                return cronograma;
            }
            catch (Exception ex)
            {
                // Log de error táctico
                throw new Exception($"Error en la extracción analítica del folio {idCredito}: {ex.Message}");
            }
        }
    }
}

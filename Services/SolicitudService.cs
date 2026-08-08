using CooperativaApp.Data;
using CooperativaApp.DTOs;
using CooperativaApp.DTOS;
using CooperativaApp.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // 👈 Super Senior: Usa ILogger
using System;

namespace CooperativaApp.Services
{
    public class SolicitudService : ISolicitudService
    {
        private readonly CooperativaContext _context;
        private readonly ILogger<SolicitudService> _logger; // 👈 Inyecta logs profesionales

        public SolicitudService(CooperativaContext context, ILogger<SolicitudService> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<int> RegistrarSolicitudAsync(SolicitudCreateDTO dto, int usuarioId)
        {
            // 🛡️ REGLA DE ORO: No permitir duplicados en proceso
            var tienePendiente = await _context.Solicitudes
                .AnyAsync(s => s.SocioId == dto.SocioId && (s.Estado == "REGISTRADA" || s.Estado == "EVALUACION"));

            if (tienePendiente)
                throw new InvalidOperationException("El socio ya cuenta con una solicitud activa.");

            // 🔍 Localizar Producto y sus Tasas
            var producto = await _context.Productos.Include(p => p.Tasas)
                .FirstOrDefaultAsync(p => p.Id == dto.ProductoId);

            if (producto == null)
                throw new KeyNotFoundException("El producto seleccionado no es válido.");

            // 🧮 Calcular Tasa según los rangos del producto (Uso de tu lógica de umbrales)
            var tasa = producto.Tasas
                .Where(t => dto.Monto >= t.MontoMinimo && dto.Monto <= t.MontoMaximo)
                .Select(t => t.TasaInteres).FirstOrDefault();

            // Si no cae en ningún rango, usamos la referencial
            if (tasa == 0) tasa = producto.TasaReferencial ?? 0;

            // 📝 Mapeo a la Entidad SolicitudCredito
            var solicitud = new SolicitudCredito
            {
                SocioId = dto.SocioId,
                ProductoId = dto.ProductoId,
                MontoSolicitado = dto.Monto,
                PlazoMeses = dto.Plazo,
                TasaAplicada = tasa,
                Estado = "REGISTRADA",
                FechaCreacion = DateTime.Now,
                UsuarioCreadorId = usuarioId, // 👈 Auditoría Titanium
                Observaciones = "Solicitud generada desde el Simulador Pro"
            };

            _context.Solicitudes.Add(solicitud);
            await _context.SaveChangesAsync();

            return solicitud.Id;
        }
        public async Task<IEnumerable<SolicitudPendienteDTO>> ObtenerPendientesAsync(decimal montoMaximoAutorizado)
        {
            // 🛡️ AJUSTE ANTIDESBORDAMIENTO TITANIUM
            decimal umbralSeguro = montoMaximoAutorizado <= 0 || montoMaximoAutorizado > 100_000_000m
                                    ? 100_000_000m
                                    : montoMaximoAutorizado;

            // 🔍 JOIN DIAMANTE: Usando las columnas reales sol.TasaAplicada y sol.PlazoMeses
            var query = await (from sol in _context.Solicitudes
                               join soc in _context.Socios on sol.SocioId equals soc.IdSocio
                               join prod in _context.Productos on sol.ProductoId equals prod.Id
                               where (sol.Estado == "REGISTRADA" || sol.Estado == "OBSERVADA")
                               && sol.MontoSolicitado <= umbralSeguro
                               // 🎯 1. ORDENAMIENTO REQUERIDO POR APELLIDO PATERNO
                               orderby soc.ApellidoPaterno, soc.ApellidoMaterno, soc.Nombres
                               select new
                               {
                                   sol,
                                   soc,
                                   ProductoNombre = prod.Nombre,
                                   SistemaAmortizacion = prod.CalculoCuota
                               }).ToListAsync();

            return query.Select(x => {
                decimal monto = x.sol.MontoSolicitado;
                // 🎯 Usamos la propiedad real de la BD: TasaAplicada
                decimal tasaPercent = x.sol.TasaAplicada;
                // 🎯 Usamos la propiedad real de la BD: PlazoMeses
                int plazo = x.sol.PlazoMeses;
                string sistema = (x.SistemaAmortizacion ?? "FRANCES").ToUpper().Trim();

                // 🎯 2. MOTOR DE CÁLCULO DE CUOTA CORREGIDO SEGÚN SISTEMA
                decimal cuotaCalculada = 0m;
                decimal tasaDecimal = tasaPercent / 100m;

                if (monto > 0 && plazo > 0)
                {
                    switch (sistema)
                    {
                        case "INTERES_UNICA":
                        case "IUNICA":
                            // 🧮 Fórmula para Interés Única: (Monto / Plazo) + (Monto * TasaMensual)
                            cuotaCalculada = (monto / plazo) + (monto * tasaDecimal);
                            break;

                        case "ALEMAN":
                            cuotaCalculada = (monto / plazo) + (monto * (tasaDecimal / 12m));
                            break;

                        case "INTERES_SIMPLE":
                            decimal intM = monto * (tasaDecimal / 12m);
                            cuotaCalculada = (plazo == 1) ? (monto + intM) : intM;
                            break;

                        case "FRANCES":
                        default:
                            double i = (double)(tasaDecimal / 12m);
                            if (i == 0)
                            {
                                cuotaCalculada = monto / plazo;
                            }
                            else
                            {
                                double cDbl = (double)monto * (i * Math.Pow(1 + i, plazo)) / (Math.Pow(1 + i, plazo) - 1);
                                cuotaCalculada = (decimal)cDbl;
                            }
                            break;
                    }
                }

                // 🎯 3. INSTANCIACIÓN DEL RECORD POSICIONAL CON ATRIBUTOS DE TU TABLA
                return new SolicitudPendienteDTO(
                    x.sol.Id,
                    x.soc.IdSocio, // 👈 Inyección de SocioId para la matriz de riesgos en React
                    $"{x.soc.ApellidoPaterno} {x.soc.ApellidoMaterno} {x.soc.Nombres}".Trim().ToUpper(),
                    x.ProductoNombre.ToUpper(),
                    monto,
                    plazo,
                    tasaPercent,
                    Math.Round(cuotaCalculada, 2),
                    x.sol.Estado ?? "REGISTRADA",
                    x.sol.FechaCreacion ?? DateTime.Now,
                    sistema
                );
            });
        }
        public async Task<AprobacionResponse> AprobarConSPAsync(int solicitudId, int usuarioId, string comentario)
        {
            try
            {
                // El resultado de FromSqlInterpolated se mapea a la clase AprobacionResponse
                var resultado = await _context.Set<AprobacionResponse>()
                    .FromSqlInterpolated($"EXEC [dbo].[usp_AprobarSolicitudIntegral] @IdSolicitud={solicitudId}, @UsuarioId={usuarioId}, @Comentario={comentario}")
                    .ToListAsync();

                // Devolvemos el objeto encontrado o uno de error, ambos son tipo AprobacionResponse
                return resultado.FirstOrDefault() ?? new AprobacionResponse(0, "No se recibió respuesta del motor", false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falla crítica en Motor de Créditos para Solicitud {Id}", solicitudId);
                return new AprobacionResponse(0, $"Error: {ex.Message}", false);
            }
        }
        public async Task<AprobacionResponse> AprobarYGenerarCreditoAsync(int idSolicitud, int usuarioId, string comentario)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. SP Simple: Solo inserta en Creditos y cambia estado a APROBADO en Solicitudes
                // Debe devolver el ID del crédito generado
                var paramId = new SqlParameter("@IdSolicitud", idSolicitud);
                var paramUser = new SqlParameter("@UsuarioId", usuarioId);
                var paramCom = new SqlParameter("@Comentario", comentario);

                var resultado = await _context.Database
                    .SqlQueryRaw<int>("EXEC usp_AprobarSolicitudCabecera @IdSolicitud, @UsuarioId, @Comentario",
                                       paramId, paramUser, paramCom)
                    .ToListAsync();

                int idNuevoCredito = resultado.FirstOrDefault();

                if (idNuevoCredito <= 0) throw new Exception("Error al crear cabecera de crédito.");

                // 🚀 2. MOTOR DIAMANTE C#: Generación de Cuotas Multimotor
                // Este método ya tiene el switch (Frances, Aleman, Simple)
                await GenerarCuotasAsync(idNuevoCredito);

                await transaction.CommitAsync();
                return new AprobacionResponse(idNuevoCredito, "Crédito y Cronograma Generados en C#", true);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"🚨 Error: {ex.Message}");
                return new AprobacionResponse(0, ex.Message, false);
            }
        }
        public async Task<AprobacionResponse> DecidirSolicitudAsync(int solicitudId, DecisionRequestDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Ejecutar SP enviando el IdSocioAval
                var resultadoSP = await _context.Set<AprobacionResponse>()
                    .FromSqlInterpolated($"EXEC [dbo].[usp_AprobarSolicitudIntegral] @IdSolicitud={solicitudId}, @UsuarioId={dto.UsuarioId}, @Comentario={dto.Comentario}, @Accion={dto.Accion}, @IdSocioAval={dto.IdSocioAval}")
                    .ToListAsync();

                var respuesta = resultadoSP.FirstOrDefault() ?? new AprobacionResponse(0, "Error en SP", false);

                if (respuesta.Exito && dto.Accion.ToUpper() == "APROBAR" && respuesta.IdCreditoGenerado > 0)
                {
                    // 🎯 Asignar el Aval directamente al crédito generado si el SP no lo hizo
                    if (dto.IdSocioAval.HasValue && dto.IdSocioAval.Value > 0)
                    {
                        var credito = await _context.Creditos.FindAsync(respuesta.IdCreditoGenerado);
                        if (credito != null)
                        {
                            credito.IdSocioAval = dto.IdSocioAval.Value;
                        }
                    }

                    // Generar cuotas
                    await GenerarCuotasSinTransaccionAsync(respuesta.IdCreditoGenerado);
                }

                await transaction.CommitAsync();
                return respuesta;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Falla crítica en DecidirSolicitud {Id}", solicitudId);
                return new AprobacionResponse(0, $"Error: {ex.Message}", false);
            }
        }
        // ⚙️ MÉTODO AUXILIAR SIN TRANSACCIÓN PROPIA
        private async Task GenerarCuotasSinTransaccionAsync(int idCredito)
        {
            var credito = await _context.Creditos.FindAsync(idCredito);
            if (credito == null) throw new Exception("Crédito no encontrado.");

            // Limpieza de seguridad
            var antiguas = _context.Cuotas.Where(x => x.IdCredito == idCredito);
            _context.Cuotas.RemoveRange(antiguas);
            await _context.SaveChangesAsync();

            string sistema = credito.TipoCalculo?.Trim().ToUpper() ?? "FRANCES";

            switch (sistema)
            {
                case "ALEMAN": GenerarAleman(credito); break;
                case "INTERES_SIMPLE": GenerarInteresSimple(credito); break;
                case "IUNICA": GenerarIUnica(credito); break;
                case "INTERES_UNICA": GenerarIUnica(credito); break;
                    
                default: GenerarFrances(credito); break;
            }
            await _context.SaveChangesAsync();
        }

        public async Task GenerarCuotasAsync(int idCredito)
        {
            // 🛡️ Iniciamos transacción para asegurar consistencia atómica
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var credito = await _context.Creditos
                    .FirstOrDefaultAsync(x => x.IdCredito == idCredito);

                if (credito == null) throw new Exception("Crédito no detectado en el Core.");

                // 🔹 Limpieza de cronogramas previos (Idempotencia)
                var cuotasAntiguas = _context.Cuotas.Where(x => x.IdCredito == idCredito);
                _context.Cuotas.RemoveRange(cuotasAntiguas);
                await _context.SaveChangesAsync();

                // ⚙️ Selección Dinámica de Motor
                string sistema = credito.TipoCalculo?.Trim().ToUpper() ?? "FRANCES";

                switch (sistema)
                {
                    case "ALEMAN":
                        GenerarAleman(credito);
                        break;
                    case "INTERES_SIMPLE":
                        GenerarInteresSimple(credito);
                        break;
                    case "IUNICA":
                        GenerarIUnica(credito);
                        break;
                    case "INTERES_UNICA":
                        GenerarIUnica(credito);
                        break;
                    case "FRANCES":
                    default:
                        GenerarFrances(credito);
                        break;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync(); // 🚀 Sincronización exitosa
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Falla Crítica en Motor Financiero: {ex.Message}");
                throw;
            }
        }

        private void GenerarFrances(Credito credito)
        {
            decimal P = credito.Monto;
            decimal tea = credito.TasaInteres / 100; // TEA como decimal, ej: 0.24
            int n = credito.PlazoMeses;
            int diasPorCuota = 30; // días entre cuotas (configurable)
            int baseCalculo = 360; // base comercial Peru — cambiar a 365 si aplica

            // ── PASO 1: Días acumulados por cuota ──
            int[] diasAcumulados = new int[n];
            for (int k = 0; k < n; k++)
                diasAcumulados[k] = (k + 1) * diasPorCuota;

            // ── PASO 2: Factor de descuento por cuota ──
            // Factor_i = 1 / (1 + TEA) ^ (diasAcumulados_i / baseCalculo)
            double[] factores = new double[n];
            for (int k = 0; k < n; k++)
                factores[k] = 1.0 / Math.Pow(1.0 + (double)tea, (double)diasAcumulados[k] / baseCalculo);

            // ── PASO 3: Cuota fija = Capital / Suma de factores ──
            double sumaFactores = 0;
            foreach (var f in factores) sumaFactores += f;
            decimal cuotaFija = Math.Round(P / (decimal)sumaFactores, 2);

            // ── PASO 4: Generar cronograma ──
            decimal saldoRestante = P;

            for (int k = 1; k <= n; k++)
            {
                // Días del período actual
                int diasPeriodo = diasPorCuota; // siempre 30 si cuotas son mensuales fijas

                // Tasa del período: (1 + TEA)^(díasPeriodo/base) - 1
                decimal tasaPeriodo = (decimal)(Math.Pow(1.0 + (double)tea, (double)diasPeriodo / baseCalculo) - 1.0);

                decimal interes = Math.Round(saldoRestante * tasaPeriodo, 2);

                decimal capital;
                if (k == n)
                {
                    // Última cuota: liquidar saldo exacto para evitar diferencias por redondeo
                    capital = Math.Round(saldoRestante, 2);
                    saldoRestante = 0;
                }
                else
                {
                    capital = Math.Round(cuotaFija - interes, 2);
                    saldoRestante = Math.Round(saldoRestante - capital, 2);
                }

                _context.Cuotas.Add(CrearCuotaBase(
                    credito.IdCredito,
                    k,
                    Math.Abs(capital),
                    Math.Abs(interes),
                    Math.Max(0, saldoRestante)
                ));
            }
        }
        private void GenerarAleman(Credito credito)
        {
            decimal P = credito.Monto;
            decimal tea = credito.TasaInteres / 100;
            int n = credito.PlazoMeses;
            int diasPorCuota = 30;
            int baseCalculo = 360;

            // Capital fijo por cuota — en alemán siempre es P/n
            decimal capitalFijo = Math.Round(P / n, 2);
            decimal saldoRestante = P;

            for (int k = 1; k <= n; k++)
            {
                // Tasa efectiva del período
                decimal tasaPeriodo = (decimal)(Math.Pow(1.0 + (double)tea, (double)diasPorCuota / baseCalculo) - 1.0);

                decimal interes = Math.Round(saldoRestante * tasaPeriodo, 2);

                decimal capital;
                if (k == n)
                {
                    // Última cuota: liquidar saldo exacto
                    capital = Math.Round(saldoRestante, 2);
                    saldoRestante = 0;
                }
                else
                {
                    capital = capitalFijo;
                    saldoRestante = Math.Round(saldoRestante - capital, 2);
                }

                decimal cuotaMonto = Math.Round(Math.Abs(capital) + Math.Abs(interes), 2);

                _context.Cuotas.Add(CrearCuotaBase(
                    credito.IdCredito,
                    k,
                    Math.Abs(capital),
                    Math.Abs(interes),
                    Math.Max(0, saldoRestante),
                    cuotaMonto
                ));
            }
        }
        private void GenerarIUnica(Credito credito)
        {
            decimal principal = credito.Monto;
            decimal tasaDirectaMensual = credito.TasaInteres / 100; // 🎯 Directa mensual (ej: 2.5% -> 0.025). ¡Ya no se divide entre 12!
            int n = credito.PlazoMeses;

            // ── PASO 1: Calcular la masa de interés decreciente "fantasma" ──
            decimal capitalFijoTeorico = principal / n;
            decimal saldoIteracion = principal;
            decimal[] listaInteresesCalculados = new decimal[n];
            decimal sumaInteresesTotal = 0;

            for (int i = 0; i < n; i++)
            {
                decimal interesMes = Math.Round(saldoIteracion * tasaDirectaMensual, 2);
                listaInteresesCalculados[i] = interesMes;
                sumaInteresesTotal += interesMes;

                saldoIteracion -= capitalFijoTeorico;
            }

            // ── PASO 2: Fijar el Crédito Total y la Cuota Fija Mensual Definitiva ──
            decimal creditoTotalAPagar = Math.Round(principal + sumaInteresesTotal, 2);
            decimal cuotaFijaMensual = Math.Round(creditoTotalAPagar / n, 2);

            // ── PASO 3: Construcción real del plan con capital inverso y persistencia ──
            decimal saldoCapitalReal = principal;

            for (int k = 1; k <= n; k++)
            {
                decimal interesMes = listaInteresesCalculados[k - 1];

                // 🎯 Regla de negocio solicitada: El capital varía = Cuota Fija - Interés Inicialmente Calculado
                decimal capitalAjustado = Math.Round(cuotaFijaMensual - interesMes, 2);

                // 🛡️ Blindaje contra redondeo: La última cuota absorbe el saldo restante exacto
                if (k == n)
                {
                    capitalAjustado = Math.Round(saldoCapitalReal, 2);
                    saldoCapitalReal = 0;
                }
                else
                {
                    saldoCapitalReal = Math.Round(saldoCapitalReal - capitalAjustado, 2);
                }

                // Persistimos en la base de datos usando tu método base
                _context.Cuotas.Add(CrearCuotaBase(
                    credito.IdCredito,
                    k,
                    Math.Abs(capitalAjustado),
                    Math.Abs(interesMes),
                    Math.Max(0, saldoCapitalReal)
                ));
            }
        }
        private void GenerarInteresSimple(Credito credito)
        {
            decimal P = credito.Monto;
            decimal tea = credito.TasaInteres / 100;
            int n = credito.PlazoMeses;
            int diasPorCuota = 30;
            int baseCalculo = 360;

            // Tasa efectiva del período (igual para todas las cuotas)
            decimal tasaPeriodo = (decimal)(Math.Pow(1.0 + (double)tea, (double)diasPorCuota / baseCalculo) - 1.0);

            // Interés simple: se calcula siempre sobre el capital original (no sobre saldo)
            decimal interesFijo = Math.Round(P * tasaPeriodo, 2);

            decimal saldoRestante = P;

            for (int k = 1; k <= n; k++)
            {
                // Bullet: capital solo en última cuota, resto paga solo interés
                decimal capital;
                if (k == n)
                {
                    capital = Math.Round(saldoRestante, 2);
                    saldoRestante = 0;
                }
                else
                {
                    capital = 0;
                    // saldoRestante no cambia hasta la última cuota
                }

                decimal cuotaMonto = Math.Round(Math.Abs(capital) + interesFijo, 2);

                _context.Cuotas.Add(CrearCuotaBase(
                    credito.IdCredito,
                    k,
                    Math.Abs(capital),
                    interesFijo,
                    Math.Max(0, saldoRestante),
                    cuotaMonto
                ));
            }
        }
        // 🛠️ Helper para evitar duplicidad de código (DRY)
        private Cuota CrearCuotaBase(int idCredito, int num, decimal cap, decimal intrs, decimal saldo, decimal? montoManual = null)
        {
            decimal total = montoManual ?? (cap + intrs);
            return new Cuota
            {
                IdCredito = idCredito,
                NumeroCuota = num,
                FechaVencimiento = DateTime.Today.
                AddMonths(num),
                Capital = cap,
                Interes = intrs,
                MontoCuota = total,
                SaldoCapital = Math.Max(0, cap),
                SaldoInteres= intrs,
                Estado = "PENDIENTE",
                Saldo = total // Lo que el socio debe pagar por esta cuota
            };
        }
        public async Task<IEnumerable<SolicitudDetalleDTO>> ListarTodasAsync()
        {
            return await _context.Solicitudes
                .Include(s => s.Socio)    // 📡 Carga la tabla Socio
                .Include(s => s.Producto) // 📡 Carga la tabla Producto
                .OrderByDescending(s => s.Id)
                .Select(s => new SolicitudDetalleDTO
                {
                    Id = s.Id,
                    // 🛡️ Usamos operadores de seguridad por si Socio o Producto son nulos
                    SocioNombre = s.Socio != null ? s.Socio.Nombres + " " + s.Socio.Apellidos : "Socio No Registrado",
                    ProductoNombre = s.Producto != null ? s.Producto.Nombre : "Crédito General",

                    // 🛡️ Ajuste de nombres (Cámbialos por los nombres reales de tu modelo)
                    Monto = s.MontoSolicitado, // 👈 Si falla, cámbialo por s.MontoSolicitado o el nombre real
                    Estado = s.Estado ?? "PENDIENTE",
                    Plazo = s.PlazoMeses,
                    TasaReferencial = s.TasaAplicada,

                    // 🛡️ Fix del DateTime (System.DateTime? -> System.DateTime)
                    FechaCreacion = s.FechaCreacion ?? DateTime.Now,

                    IdentidadSocio = s.Socio != null ? s.Socio.Nombres + " " + s.Socio.Apellidos  : "EXTERNO"
                })
                .ToListAsync();
        }
        public async Task<IEnumerable<SolicitudDetalleDTO>> ObtenerPorSocioAsync(int socioId)
        {
            // 🛰️ 1. RADAR DE NÚCLEO FAMILIAR (Protocolo Invisible)
            var idsFamilia = await _context.Familiaridad
                .Where(f => f.IdSocioTitular == socioId && f.Activo)
                .Select(f => f.IdSocioFamiliar)
                .ToListAsync();
            idsFamilia.Add(socioId);

            // 🛰️ 2. EXTRACCIÓN Y MAPEO TITANIUM
            var query = await _context.Solicitudes
                .Include(s => s.Producto)
                .Include(s => s.Socio)
                .Where(s => idsFamilia.Contains(s.SocioId))
                .OrderByDescending(s => s.Id)
                .Select(s => new SolicitudDetalleDTO
                {
                    Id = s.Id,
                    SocioId = s.SocioId,
                    SocioNombre = s.Socio != null ? (s.Socio.Nombres + " " + s.Socio.Apellidos).ToUpper() : "SOCIO",
                    ProductoNombre = s.Producto != null ? s.Producto.Nombre : "CRÉDITO PERSONAL",

                    // 💰 PARÁMETROS ECONÓMICOS (Fuerza Bruta de Mapeo)
                    Monto = s.MontoSolicitado,

                    // 🎯 Si PlazoMeses devuelve 0, intentamos mapear directamente 
                    // asegurándonos que el DTO reciba el valor de la propiedad del modelo
                    Plazo = s.PlazoMeses,
                    TasaReferencial = s.TasaAplicada,

                    Estado = s.Estado ?? "REGISTRADA",
                    FechaCreacion = s.FechaCreacion ?? DateTime.Now,
                    ComentarioAnalista = s.ComentarioEvaluador,
                    TipoAmortizacion = "FRANCÉS" // Valor por defecto si es nulo
                })
                .ToListAsync();

            return query;
        }
        //*****************************
        public async Task<object> ObtenerAnalisisRiesgoSocioAsync(int idSocio)
        {
            var socio = await _context.Socios
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdSocio == idSocio);

            var creditosSocio = await _context.Creditos
                .Where(c => c.IdSocio == idSocio)
                .AsNoTracking()
                .ToListAsync();

            var idsCreditos = creditosSocio.Select(c => c.IdCredito).ToList();

            var cuotasTodas = await _context.Cuotas
                .Where(q => idsCreditos.Contains(q.IdCredito))
                .AsNoTracking()
                .ToListAsync();

            var creditosDetalle = new List<object>();
            int totalCuotasConMoraGlobal = 0;
            int creditosConMoraActiva = 0;

            foreach (var c in creditosSocio)
            {
                var cuotasCredito = cuotasTodas.Where(q => q.IdCredito == c.IdCredito).ToList();

                int cuotasTotales = cuotasCredito.Count > 0 ? cuotasCredito.Count : c.PlazoMeses;
                int cuotasPagadas = cuotasCredito.Count(q => (q.Estado ?? "").ToUpper() == "PAGADO");

                // 🎯 EVALUACIÓN DE CUOTAS CON MORA PAGADA O PENDIENTE VENCIDA
                int cuotasConMoraPasada = cuotasCredito.Count(q => (q.Estado ?? "").ToUpper() == "PAGADO" && (q.MoraGenerada > 0 || q.SaldoMora > 0));

                // Próxima cuota pendiente
                var proximaCuota = cuotasCredito
                    .Where(q => (q.Estado ?? "").ToUpper() == "PENDIENTE")
                    .OrderBy(q => q.FechaVencimiento)
                    .FirstOrDefault();

                DateTime? fechaProxima = proximaCuota?.FechaVencimiento;

                // 🎯 MORA ACTIVA: Cuota pendiente con fecha menor a la fecha actual
                bool tieneCuotaVencidaPendiente = cuotasCredito.Any(q => (q.Estado ?? "").ToUpper() == "PENDIENTE" && q.FechaVencimiento.Date < DateTime.Today);

                if (tieneCuotaVencidaPendiente) creditosConMoraActiva++;
                totalCuotasConMoraGlobal += (cuotasConMoraPasada + (tieneCuotaVencidaPendiente ? 1 : 0));

                decimal capPagado = cuotasCredito.Sum(q => q.Capital - q.SaldoCapital);
                decimal intPagado = cuotasCredito.Sum(q => q.Interes - q.SaldoInteres);
                decimal moraPagada = cuotasCredito.Sum(q => q.MoraGenerada - q.SaldoMora);

                decimal saldoInteresCredito = cuotasCredito.Sum(q => q.SaldoInteres);
                decimal saldoMoraCredito = cuotasCredito.Sum(q => q.SaldoMora);
                decimal saldoCapitalCredito = cuotasCredito.Sum(q => q.SaldoCapital);
                // Determinación de calificación de la operación
                string calificacionOperacion = "EXCELENTE";
                if (tieneCuotaVencidaPendiente) calificacionOperacion = "EN_MORA";
                else if (cuotasConMoraPasada > 0) calificacionOperacion = "OBSERVADO";

                string estadoFormateado = (c.EstadoCredito ?? c.Estado ?? "VIGENTE").ToUpper();

                creditosDetalle.Add(new
                {
                    idCredito = c.IdCredito,
                    producto = "CRÉDITO DE CARTERA",
                    montoOtorgado = c.Monto,
                    plazoMeses = c.PlazoMeses,
                    cuotasTotales = cuotasTotales,
                    cuotasPagadas = cuotasPagadas,
                    cuotasConMora = cuotasConMoraPasada + (tieneCuotaVencidaPendiente ? 1 : 0),
                    tieneCuotaVencidaPendiente = tieneCuotaVencidaPendiente,
                    calificacionOperacion = calificacionOperacion,
                    saldoCapital = saldoCapitalCredito,
                    saldoInteres = saldoInteresCredito,
                    saldoMora = saldoMoraCredito,
                    capitalPagado = capPagado > 0 ? capPagado : (c.Monto - c.SaldoCapital),
                    interesPagado = intPagado,
                    moraPagada = moraPagada,
                    estadoCredito = estadoFormateado,
                    fechaDesembolso = c.FechaUltimoDesembolso ?? c.FechaDesembolso,
                    fechaProximoVencimiento = fechaProxima
                });
            }

            var creditosVigentesCount = creditosSocio.Count(x => (x.EstadoCredito ?? x.Estado ?? "").ToUpper() == "VIGENTE" || (x.EstadoCredito ?? x.Estado ?? "").ToUpper() == "DESEMBOLSADO");
            var creditosCanceladosCount = creditosSocio.Count(x => (x.EstadoCredito ?? x.Estado ?? "").ToUpper() == "CANCELADO");
            var deudaTotalVigente = creditosSocio.Where(x => (x.EstadoCredito ?? x.Estado ?? "").ToUpper() == "VIGENTE" || (x.EstadoCredito ?? x.Estado ?? "").ToUpper() == "DESEMBOLSADO").Sum(x => x.SaldoCapital);

            // 🎯 CÁLCULO EXACTO DE PUNTUALIDAD HISTÓRICA SOBRE CUOTAS EXIGIBLES Y PAGADAS
            var cuotasPagadasTotal = cuotasTodas.Where(q => (q.Estado ?? "").ToUpper() == "PAGADO").ToList();
            var cuotasPagadasPuntuales = cuotasPagadasTotal.Count(q => q.MoraGenerada == 0);

            double scorePuntualidad = 100.0;
            if (cuotasPagadasTotal.Count > 0)
            {
                scorePuntualidad = Math.Round(((double)cuotasPagadasPuntuales / cuotasPagadasTotal.Count) * 100, 1);
            }

            // Penalización por moras pendientes actuales
            if (creditosConMoraActiva > 0)
            {
                scorePuntualidad = Math.Max(0, scorePuntualidad - (creditosConMoraActiva * 15.0));
            }

            // 🎯 REGLA DE SUGERENCIA DE DICTAMEN
            string dictamenSugerido = "APROBAR_DIRECTO";
            if (creditosConMoraActiva > 0 || totalCuotasConMoraGlobal > 1)
            {
                dictamenSugerido = "REQUERIR_AVAL";
            }
            if (creditosConMoraActiva >= 2)
            {
                dictamenSugerido = "RECHAZAR";
            }

            return new
            {
                idSocio = idSocio,
                nombreSocio = socio != null ? $"{socio.ApellidoPaterno} {socio.ApellidoMaterno} {socio.Nombres}".Trim().ToUpper() : "SOCIO",
                creditosVigentesCount = creditosVigentesCount,
                creditosCanceladosCount = creditosCanceladosCount,
                deudaTotalVigente = deudaTotalVigente,
                scorePuntualidad = Math.Round(scorePuntualidad, 1),
                dictamenSugerido = dictamenSugerido,
                creditos = creditosDetalle
            };
        }
        public async Task<object> ValidarSocioAvalAsync(string dni)
        {
            var socio = await _context.Socios
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.DNI == dni);

            if (socio == null)
            {
                return new { exito = false, mensaje = "El DNI ingresado no pertenece a ningún socio registrado." };
            }

            return new
            {
                exito = true,
                idSocio = socio.IdSocio,
                nombreCompleto = $"{socio.ApellidoPaterno} {socio.ApellidoMaterno} {socio.Nombres}".Trim().ToUpper(),
                dni = socio.DNI
            };
        }

    }
}
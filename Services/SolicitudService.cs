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

            // 🔍 JOIN DIAMANTE: Extraemos 'CalculoCuota' del producto
            var query = await (from sol in _context.Solicitudes
                               join soc in _context.Socios on sol.SocioId equals soc.IdSocio
                               join prod in _context.Productos on sol.ProductoId equals prod.Id
                               where (sol.Estado == "REGISTRADA" || sol.Estado == "OBSERVADA")
                               && sol.MontoSolicitado <= umbralSeguro
                               orderby sol.FechaCreacion descending
                               select new
                               {
                                   sol,
                                   soc,
                                   ProductoNombre = prod.Nombre,
                                   SistemaAmortizacion = prod.CalculoCuota // 🛡️ Captura ALEMAN, FRANCES o INTERES_SIMPLE
                               }).ToListAsync();

            return query.Select(x => {
                // 🧮 MOTOR DE CÁLCULO REFERENCIAL (Mantenemos Francés solo como backup)
                double tea = (double)(x.sol.TasaAplicada > 0 ? x.sol.TasaAplicada : 0) / 100;
                double tem = Math.Pow(1 + tea, 1.0 / 12.0) - 1;
                double cuota = 0;

                if (tem > 0 && x.sol.PlazoMeses > 0)
                {
                    cuota = (double)x.sol.MontoSolicitado * (tem / (1 - Math.Pow(1 + tem, -x.sol.PlazoMeses)));
                }

                return new SolicitudPendienteDTO(
                    x.sol.Id,
                    $"{x.soc.Nombres} {x.soc.ApellidoPaterno} {x.soc.ApellidoMaterno}".Trim(),
                    x.ProductoNombre,
                    x.sol.MontoSolicitado,
                    x.sol.PlazoMeses,
                    x.sol.TasaAplicada,
                    (decimal)Math.Round(cuota, 2),
                    x.sol.Estado ?? "REGISTRADA",
                    x.sol.FechaCreacion ?? DateTime.Now,
                    x.SistemaAmortizacion ?? "FRANCES" // 🚀 Inyección del sistema real
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
            // 🛡️ ÚNICA TRANSACCIÓN MAESTRA
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Ejecutar SP (Asegúrate de que el SP NO tenga su propio Begin/Commit interno si es posible, o que sea compatible)
                var resultadoSP = await _context.Set<AprobacionResponse>()
                    .FromSqlInterpolated($"EXEC [dbo].[usp_AprobarSolicitudIntegral] @IdSolicitud={solicitudId}, @UsuarioId={dto.UsuarioId}, @Comentario={dto.Comentario}, @Accion={dto.Accion}")
                    .ToListAsync();

                var respuesta = resultadoSP.FirstOrDefault() ?? new AprobacionResponse(0, "Error en SP", false);

                if (respuesta.Exito && dto.Accion.ToUpper() == "APROBAR" && respuesta.IdCreditoGenerado > 0)
                {
                    // 🚀 Invocamos el generador SIN que este abra otra transacción
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
            decimal i = (credito.TasaInteres / 100) / 12; // Tasa Mensualizada
            int n = credito.PlazoMeses;

            double factor = Math.Pow(1 + (double)i, n);
            decimal cuotaFija = Math.Round(P * (i * (decimal)factor) / ((decimal)factor - 1), 2);

            decimal saldoRestante = P;
            for (int k = 1; k <= n; k++)
            {
                decimal interes = Math.Round(saldoRestante * i, 2);
                decimal capital = (k == n) ? saldoRestante : (cuotaFija - interes);

                saldoRestante -= capital;

                _context.Cuotas.Add(CrearCuotaBase(credito.IdCredito, k, capital, interes, saldoRestante));
            }
        }

        private void GenerarAleman(Credito credito)
        {
            decimal P = credito.Monto;
            decimal i = (credito.TasaInteres / 100) / 12;
            int n = credito.PlazoMeses;

            decimal capitalFijo = Math.Round(P / n, 2);
            decimal saldoRestante = P;

            for (int k = 1; k <= n; k++)
            {
                decimal interes = Math.Round(saldoRestante * i, 2);
                decimal capital = (k == n) ? saldoRestante : capitalFijo;
                decimal cuotaMonto = capital + interes;

                saldoRestante -= capital;

                _context.Cuotas.Add(CrearCuotaBase(credito.IdCredito, k, capital, interes, saldoRestante, cuotaMonto));
            }
        }

        private void GenerarInteresSimple(Credito credito)
        {
            decimal P = credito.Monto;
            decimal i = (credito.TasaInteres / 100) / 12;
            int n = credito.PlazoMeses;

            decimal interesMensual = Math.Round(P * i, 2);
            decimal saldoRestante = P;

            for (int k = 1; k <= n; k++)
            {
                // En Interés Simple (Bullet), solo se paga interés y el capital va al final
                decimal capital = (k == n) ? P : 0;
                decimal cuotaMonto = capital + interesMensual;

                if (k == n) saldoRestante = 0;

                _context.Cuotas.Add(CrearCuotaBase(credito.IdCredito, k, capital, interesMensual, saldoRestante, cuotaMonto));
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
                .Select(f => f.IdFamiliaridad)
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
    }
}
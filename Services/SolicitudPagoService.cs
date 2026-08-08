using CooperativaApp.Data;
using CooperativaApp.DTOS;
using CooperativaApp.Utils;
using CooperativaApp.Interfaces;
using CooperativaApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Transactions;

namespace CooperativaApp.Services
{
    public class SolicitudPagoService : ISolicitudPagoService
    {
        private readonly CooperativaContext _context;

        private readonly BlobStorageService _blobService;

        public SolicitudPagoService(CooperativaContext context, BlobStorageService blobService)
        {
            _context = context;
            _blobService = blobService;
        }

        public async Task<IEnumerable<object>> ObtenerPendientesAsync()
        {
            return await _context.SolicitudPagoSocio
                .Include(s => s.Socio)
                .Include(s => s.MediosPago) // 📡 Carga la tabla Producto
                .Where(s => s.IdEstado == 1)
                .OrderByDescending(s => s.FechaSolicitud)
                .Select(s => new {
                    s.IdSolicitud,
                    s.IdCredito,
                    s.Monto,
                    MedioPago=s.MediosPago != null ? s.MediosPago.Nombre : "Medio Desconocido",
                    s.ReferenciaOperacion,
                    s.FechaSolicitud,
                    NombreSocio = s.Socio != null ? s.Socio.Nombres + " " + s.Socio.Apellidos : "Socio Desconocido",
                    DniSocio = s.Socio != null ? s.Socio.DNI : "",
                    ComprobanteUrl=s.ComprobanteUrl,
                    EsPrecancelacion = s.EsPrecancelacion ?? false
                }).ToListAsync();
        }

        public async Task<OperacionResponse> ProcesarSolicitudAsync(int idSolicitud, string accion, string? motivo, int usuarioId)
        {
            var solicitud = await _context.SolicitudPagoSocio.FindAsync(idSolicitud);
            if (solicitud == null) return new OperacionResponse(false, "Solicitud no encontrada.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (accion.ToUpper() == "APROBAR")
                {
                    List<OperacionResponse> res;

                    // 🎯 Bifurcación por tipo de transacción
                    if (solicitud.EsPrecancelacion??false)
                    {
                        // Invocamos el SP Maestro de Precancelación
                        res = await _context.Set<OperacionResponse>()
                            .FromSqlInterpolated($@"EXEC [dbo].[usp_ProcesarPrecancelacionMegaDiamanteWithCancelacion] 
                        @IdCredito={solicitud.IdCredito}, 
                        @IdSocio={solicitud.IdSocio},
                        @MontoAPagar={solicitud.Monto}, 
                        @IdUsuario={usuarioId}, 
                        @IdCaja=1, 
                        @ModalidadPago={solicitud.MedioPago}")
                            .ToListAsync();
                    }
                    else
                    {
                        // Invocamos el SP Maestro de Pago Regular
                        res = await _context.Set<OperacionResponse>()
                            .FromSqlInterpolated($@"EXEC [dbo].[usp_ProcesarPagoMegaDiamante] 
                        @IdCredito={solicitud.IdCredito}, 
                        @IdSocio={solicitud.IdSocio},
                        @MontoAPagar={solicitud.Monto}, 
                        @IdUsuario={usuarioId}, 
                        @IdCaja=1, 
                        @ModalidadPago={solicitud.MedioPago}")
                            .ToListAsync();
                    }

                    var respuestaSP = res.FirstOrDefault();
                    if (respuestaSP != null && respuestaSP.Exito)
                    {
                        solicitud.IdEstado = 2; // PROCESADO
                    }
                    else
                    {
                        throw new Exception(respuestaSP?.Mensaje ?? "Error al procesar en BD.");
                    }
                }
                else
                {
                    solicitud.IdEstado = 3; // RECHAZADO
                    solicitud.ObservacionesCajero = motivo;
                }

                solicitud.FechaProcesamiento = DateTimeUtils.ObtenerHoraPeru();
                solicitud.IdUsuarioCajero = usuarioId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new OperacionResponse(true, accion == "APROBAR"
                    ? (solicitud.EsPrecancelacion??false ? "Precancelación aprobada. Crédito CANCELADO y Cuotas marcadas." : "Pago aplicado al cronograma")
                    : "Solicitud rechazada");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new OperacionResponse(false, ex.Message);
            }
        }
        public async Task<OperacionResponse> CrearSolicitudSocioAsync(RegistrarSolicitudPagoDTO dto, string perfil, int? socioId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Autorización
                bool esAdmin = perfil.Equals("Administrador", StringComparison.OrdinalIgnoreCase) ||
                               perfil.Equals("Admin", StringComparison.OrdinalIgnoreCase);

                if (!esAdmin)
                {
                    if (!socioId.HasValue) return new OperacionResponse(false, "Socio no identificado.");

                    var creditoPertenece = await _context.Creditos
                        .AnyAsync(c => c.IdCredito == dto.IdCredito && c.IdSocio == socioId.Value);

                    if (!creditoPertenece)
                        return new OperacionResponse(false, "Seguridad: El crédito seleccionado no le pertenece.");
                }

                var creditoTarget = await _context.Creditos.FindAsync(dto.IdCredito);
                if (creditoTarget == null) return new OperacionResponse(false, "El crédito no existe.");

                if (creditoTarget.Estado == "CANCELADO")
                    return new OperacionResponse(false, "El crédito ya se encuentra totalmente cancelado.");

                // 🎯 1. MULTI-UPLOAD DE VOUCHERS CONCATENADOS POR COMA
                List<string> urlsSubidas = new List<string>();

                if (dto.ArchivosVouchers != null && dto.ArchivosVouchers.Any())
                {
                    foreach (var file in dto.ArchivosVouchers)
                    {
                        if (file.Length > 0)
                        {
                            string url = await _blobService.UploadVoucherAsync(file);
                            if (!string.IsNullOrEmpty(url)) urlsSubidas.Add(url);
                        }
                    }
                }
                else if (dto.ArchivoVoucher != null && dto.ArchivoVoucher.Length > 0)
                {
                    string url = await _blobService.UploadVoucherAsync(dto.ArchivoVoucher);
                    if (!string.IsNullOrEmpty(url)) urlsSubidas.Add(url);
                }

                string? comprobanteUrlFinal = urlsSubidas.Any() ? string.Join(",", urlsSubidas) : null;

                // 🎯 2. HORA OFICIAL DE PERÚ (UTC-5)
                DateTime horaPeruNow = DateTimeUtils.ObtenerHoraPeru();

                // 2. Registro Cabecera
                var nuevaSolicitud = new SolicitudPagoSocio
                {
                    IdCredito = dto.IdCredito,
                    IdSocio = creditoTarget.IdSocio,
                    Monto = dto.Monto,
                    MedioPago = dto.MedioPago,
                    IdMedioPago = dto.IdMedioPago,
                    ReferenciaOperacion = dto.Referencia,
                    ComprobanteUrl = comprobanteUrlFinal,
                    IdEstado = 1, // Pendiente
                    FechaSolicitud = horaPeruNow,
                    EsPrecancelacion = dto.EsPrecancelacionTotal
                };

                _context.SolicitudPagoSocio.Add(nuevaSolicitud);
                await _context.SaveChangesAsync();

                // 3. Imputación a Detalle
                if (dto.EsPrecancelacionTotal)
                {
                    // ⚡ MODO PRECANCELACIÓN: Se imputa Mora + Interés Vencido/Próximo + Capital Total
                    var cuotasPendientes = await _context.Cuotas
                        .Where(q => q.IdCredito == dto.IdCredito && q.Estado != "PAGADO")
                        .OrderBy(q => q.NumeroCuota)
                        .ToListAsync();

                    var proximaCuota = cuotasPendientes.FirstOrDefault(q => q.FechaVencimiento >= horaPeruNow.Date);

                    foreach (var cuota in cuotasPendientes)
                    {
                        decimal moraAPagar = cuota.SaldoMora;
                        decimal interesAPagar = 0m;

                        if (cuota.FechaVencimiento < horaPeruNow.Date || (proximaCuota != null && cuota.IdCuota == proximaCuota.IdCuota))
                        {
                            interesAPagar = cuota.SaldoInteres;
                        }

                        decimal capitalAPagar = cuota.SaldoCapital;
                        decimal totalCuotaAplicado = moraAPagar + interesAPagar + capitalAPagar;

                        _context.SolicitudPagoDetalle.Add(new SolicitudPagoDetalle
                        {
                            IdSolicitud = nuevaSolicitud.IdSolicitud,
                            IdCuota = cuota.IdCuota,
                            MontoAplicado = totalCuotaAplicado,
                            MoraCubierta = moraAPagar,
                            InteresCubierto = interesAPagar,
                            CapitalCubierto = capitalAPagar,
                            FechaSolicitud = horaPeruNow
                        });
                    }
                }
                else
                {
                    // 📊 MODO REGULAR: Amortización Libre (WaterFall sobre cronograma activo)
                    // 🎯 1. Se consultan TODAS las cuotas pendientes del crédito ordenadas por número de cuota
                    var cuotasPendientes = await _context.Cuotas
                        .Where(q => q.IdCredito == dto.IdCredito && q.Estado != "PAGADO")
                        .OrderBy(q => q.NumeroCuota)
                        .ToListAsync();

                    decimal montoRestante = dto.Monto;

                    foreach (var cuota in cuotasPendientes)
                    {
                        if (montoRestante <= 0) break;

                        // 🎯 2. Cobertura de Mora primero
                        decimal moraAPagar = Math.Min(montoRestante, cuota.SaldoMora);
                        montoRestante -= moraAPagar;

                        // 🎯 3. Cobertura de Interés
                        decimal interesAPagar = Math.Min(montoRestante, cuota.SaldoInteres);
                        montoRestante -= interesAPagar;

                        // 🎯 4. Cobertura de Capital
                        decimal capitalAPagar = Math.Min(montoRestante, cuota.SaldoCapital);
                        montoRestante -= capitalAPagar;

                        decimal totalCuotaAplicado = moraAPagar + interesAPagar + capitalAPagar;

                        // 🎯 5. Si se amortizó algún monto a esta cuota, se registra en el detalle
                        if (totalCuotaAplicado > 0)
                        {
                            _context.SolicitudPagoDetalle.Add(new SolicitudPagoDetalle
                            {
                                IdSolicitud = nuevaSolicitud.IdSolicitud,
                                IdCuota = cuota.IdCuota,
                                MontoAplicado = totalCuotaAplicado,
                                MoraCubierta = moraAPagar,
                                InteresCubierto = interesAPagar,
                                CapitalCubierto = capitalAPagar,
                                FechaSolicitud = horaPeruNow
                            });
                        }
                    }
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new OperacionResponse(true, dto.EsPrecancelacionTotal
                    ? "Solicitud de Precancelación Total registrada para revisión de caja."
                    : "Reporte de pago registrado para revisión de caja.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new OperacionResponse(false, "Error: " + ex.Message);
            }
        }
        public async Task<SimulaPrecancelacionDto> ObtenerSimulacionPrecancelacionAsync(int idCredito)
        {
            var result = await _context.Set<SimulaPrecancelacionDto>()
                .FromSqlRaw("EXEC dbo.USP_ObtenerSimulacionPrecancelacion @IdCredito = {0}", idCredito)
                .ToListAsync();

            return result.FirstOrDefault();
        }
        public async Task<object?> GetDetalleSolicitudValidacionAsync(int idSolicitud)
        {
            var solicitud = await _context.SolicitudPagoSocio
                .Include(s => s.Socio)
                .Include(s => s.Credito)
                .FirstOrDefaultAsync(s => s.IdSolicitud == idSolicitud);

            if (solicitud == null) return null;

            var detalles = await _context.SolicitudPagoDetalle
                .Include(d => d.Cuota)
                .Where(d => d.IdSolicitud == idSolicitud)
                .Select(d => new
                {
                    d.IdCuota,
                    NumeroCuota = d.Cuota != null ? d.Cuota.NumeroCuota : 0,
                    FechaVencimiento = d.Cuota != null ? d.Cuota.FechaVencimiento : (DateTime?)null,
                    SaldoCapital = d.Cuota != null ? d.Cuota.SaldoCapital : 0m,
                    SaldoInteres = d.Cuota != null ? d.Cuota.SaldoInteres : 0m,
                    SaldoMora = d.Cuota != null ? d.Cuota.SaldoMora : 0m,
                    DeudaTotalContratada = d.Cuota != null ? (d.Cuota.SaldoCapital + d.Cuota.SaldoInteres + d.Cuota.SaldoMora) : 0m,
                    MontoReportadoAplicado = d.MontoAplicado,
                    CapitalCubierto = d.CapitalCubierto,
                    InteresCubierto = d.InteresCubierto,
                    MoraCubierta = d.MoraCubierta ?? 0m
                })
                .ToListAsync();

            return new
            {
                solicitud.IdSolicitud,
                solicitud.IdCredito,
                Socio = solicitud.Socio != null ? $"{solicitud.Socio.Nombres} {solicitud.Socio.Apellidos}".Trim() : "SOCIO",
                MontoSolicitado = solicitud.Monto,
                EsPrecancelacion = solicitud.EsPrecancelacion ?? false,
                Detalles = detalles
            };
        }
    }
}
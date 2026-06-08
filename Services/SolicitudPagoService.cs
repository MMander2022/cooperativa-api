using CooperativaApp.Data;
using CooperativaApp.DTOS;
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
                .Where(s => s.IdEstado == 1)
                .OrderByDescending(s => s.FechaSolicitud)
                .Select(s => new {
                    s.IdSolicitud,
                    s.IdCredito,
                    s.Monto,
                    s.MedioPago,
                    s.ReferenciaOperacion,
                    s.FechaSolicitud,
                    NombreSocio = s.Socio != null ? s.Socio.Nombres + " " + s.Socio.Apellidos : "Socio Desconocido",
                    DniSocio = s.Socio != null ? s.Socio.DNI : "",
                    ComprobanteUrl=s.ComprobanteUrl
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
                    // Invocamos el SP Maestro de Cobro
                    var res = await _context.Set<OperacionResponse>()
                        .FromSqlInterpolated($@"EXEC [dbo].[usp_ProcesarPagoMegaDiamante] 
                            @IdCredito={solicitud.IdCredito}, 
                            @IdSocio={solicitud.IdSocio},
                            @MontoAPagar={solicitud.Monto}, 
                            @IdUsuario={usuarioId}, 
                            @IdCaja=1, 
                            @ModalidadPago={solicitud.MedioPago}")
                        .ToListAsync();

                    var respuestaSP = res.FirstOrDefault();
                    if (respuestaSP != null && respuestaSP.Exito)
                    {
                        solicitud.IdEstado = 2; // PROCESADO
                    }
                    else
                    {
                        throw new Exception(respuestaSP?.Mensaje ?? "Error en el SP de cobro");
                    }
                }
                else
                {
                    solicitud.IdEstado = 3; // RECHAZADO
                    solicitud.ObservacionesCajero = motivo;
                }

                solicitud.FechaProcesamiento = DateTime.Now;
                solicitud.IdUsuarioCajero = usuarioId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new OperacionResponse(true, accion == "APROBAR" ? "Pago aplicado al cronograma" : "Solicitud rechazada");
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
                // 1. 🛡️ Determinamos el nivel de autorización
                bool esAdmin = perfil.Equals("Administrador", StringComparison.OrdinalIgnoreCase) ||
                               perfil.Equals("Admin", StringComparison.OrdinalIgnoreCase);

                // 2. 🔍 Validación de Propiedad del Crédito
                if (!esAdmin)
                {
                    // Si es SOCIO, verificamos que el crédito realmente sea suyo
                    if (!socioId.HasValue)
                        return new OperacionResponse(false, "Socio no identificado en el sistema.");

                    var creditoPertenece = await _context.Creditos
                        .AnyAsync(c => c.IdCredito == dto.IdCredito && c.IdSocio == socioId.Value);

                    if (!creditoPertenece)
                        return new OperacionResponse(false, "Seguridad: El crédito seleccionado no le pertenece.");
                }
                // Si es Admin, saltamos la validación anterior y permitimos el registro directo.

                // 3. 📝 Registro de la Solicitud (Siembra)
                // Si es Admin, debemos obtener el IdSocio real del crédito para que la bandeja de caja sepa de quién es el pago
                var creditoTarget = await _context.Creditos.FindAsync(dto.IdCredito);
                if (creditoTarget == null) return new OperacionResponse(false, "El crédito no existe.");

                string? urlVoucher = null;
                if (dto.ArchivoVoucher != null && dto.ArchivoVoucher.Length > 0)
                {
                    urlVoucher = await _blobService.UploadVoucherAsync(dto.ArchivoVoucher);
                }
                var nuevaSolicitud = new SolicitudPagoSocio
                {
                    IdCredito = dto.IdCredito,
                    IdSocio = creditoTarget.IdSocio, // Usamos el socio real del crédito
                    Monto = dto.Monto,
                    MedioPago = dto.MedioPago,
                    IdMedioPago=dto.IdMedioPago,
                    ReferenciaOperacion = dto.Referencia,
                    ComprobanteUrl = urlVoucher,
                    IdEstado = 1, // Pendiente
                    FechaSolicitud = DateTime.Now
                };

                _context.SolicitudPagoSocio.Add(nuevaSolicitud);
                await _context.SaveChangesAsync();

                // 3. 🚀 MOTOR DE IMPUTACIÓN (Derrame FIFO)
                var cuotasPendientes = await _context.Cuotas
                    .Where(q => q.IdCredito == dto.IdCredito && q.Estado != "PAGADO")
                    .OrderBy(q => q.NumeroCuota)
                    .ToListAsync();
                decimal montoRestante = dto.Monto;

                foreach (var cuota in cuotasPendientes)
                {
                    if (montoRestante <= 0) break;

                    decimal deudaInteres = cuota.SaldoInteres;
                    decimal deudaCapital = cuota.SaldoCapital;
                    decimal deudaTotalCuota = deudaInteres + deudaCapital;

                    decimal pagoCuota = Math.Min(montoRestante, deudaTotalCuota);

                    // Reparto interno: Primero interés, luego capital
                    decimal interesAPagar = Math.Min(pagoCuota, deudaInteres);
                    decimal capitalAPagar = pagoCuota - interesAPagar;

                    _context.SolicitudPagoDetalle.Add(new SolicitudPagoDetalle
                    {
                        IdSolicitud =   nuevaSolicitud.IdSolicitud,
                        IdCuota = cuota.IdCuota,
                        MontoAplicado = pagoCuota,
                        InteresCubierto = interesAPagar,
                        CapitalCubierto = capitalAPagar
                    });

                    montoRestante -= pagoCuota;
                }

                await _context.SaveChangesAsync();
                await    transaction.CommitAsync();
                return new OperacionResponse(true, "Reporte registrado. Cuotas imputadas en revisión.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new OperacionResponse(false, "Error: " + ex.Message);
            }
        }
    }
}
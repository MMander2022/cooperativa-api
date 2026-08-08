using CooperativaApp.Data;
using CooperativaApp.DTOs;
using CooperativaApp.DTOS;
using CooperativaApp.Utils;
using CooperativaApp.Models;
using CooperativaApp.Services.Interfaces;
using CooperativaDB.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CooperativaApp.Services.Implementations
{
    public class SolicitudUtilidadService : ISolicitudUtilidadService
    {
        private readonly CooperativaContext _context;

        public SolicitudUtilidadService(CooperativaContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
        public async Task<ResumenSocioUtilidadDto> ObtenerResumenRetiroSocioAsync(int idSocio)
        {
            var fechaHoy = DateTime.Today;

            // 1. Cargar datos del Socio
            var socio = await _context.Socios
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdSocio == idSocio);

            if (socio == null)
            {
                return new ResumenSocioUtilidadDto
                {
                    SocioHabilitado = false,
                    MensajeInhabilitacion = $"El código de socio #{idSocio} no existe en la base de datos."
                };
            }

            // 2. Cargar el Periodo Activo dinámicamente desde la BD
            var periodo = await _context.PeriodosRetiroUtilidad
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Estado == "PROCESADO" || p.Estado == "HABILITADO");

            if (periodo == null)
            {
                return new ResumenSocioUtilidadDto
                {
                    DentroDeFechaVentanilla = false,
                    SocioHabilitado = true,
                    MensajeInhabilitacion = "Actualmente no existen periodos de utilidades autorizados por administración."
                };
            }

            DateTime fechaApertura = Convert.ToDateTime(periodo.FechaAperturaRetiro).Date;
            DateTime fechaCierre = Convert.ToDateTime(periodo.FechaCierreRetiro).Date;

            bool enVentanilla = (fechaHoy >= fechaApertura && fechaHoy <= fechaCierre);

            // 3. Cargar Saldo Consolidado de Utilidades del Socio
            var saldoConsolidado = await _context.UtilidadesConsolidadas
                .Where(c => c.IdSocio == socio.IdSocio && c.IdPeriodoConfig == periodo.IdPeriodoConfig)
                .Select(c => c.TotalUtilidadAcumulada > 0 ? c.TotalUtilidadAcumulada : c.SaldoUtilidad)
                .FirstOrDefaultAsync();

            if (saldoConsolidado <= 0)
            {
                var listaUtilidades = await _context.UtilidadesProcesadas
                    .Where(u => u.IdSocio == socio.IdSocio && u.IdPeriodoConfig == periodo.IdPeriodoConfig)
                    .AsNoTracking()
                    .ToListAsync();

                saldoConsolidado = listaUtilidades.Sum(u =>
                    (u.InteresMensualRepartir ?? u.UtilidadObtenida ?? u.MontoUtilidadGenerada ?? 0m)
                );
            }

            // Porcentaje Máximo de Retiro desde el periodo
            decimal pctPermitido = periodo.PorcentajeMaximoRetiro > 0 ? periodo.PorcentajeMaximoRetiro : 75.00m;
            decimal topeMaximo = Math.Round(saldoConsolidado * (pctPermitido / 100m), 2);

            // 4. Cargar Solicitudes registradas del socio con selección explícita
            var solicitudesPeriodo = await _context.SolicitudesUtilidad
                .Where(s => s.IdSocio == socio.IdSocio && s.IdPeriodoConfig == periodo.IdPeriodoConfig && s.Estado != "ELIMINADO" && s.Estado != "RECHAZADO")
                .Select(s => new SolicitudSocioHistorialDto
                {
                    IdSolicitud = s.IdSolicitud,
                    MontoSolicitado = s.MontoSolicitado,
                    TipoRetiro = s.TipoRetiro ?? "PARCIAL",
                    FechaSolicitud = Convert.ToDateTime(s.FechaSolicitud).Date,
                    Estado = s.Estado ?? "PENDIENTE",
                    ComentarioCaja = s.ComentarioCaja ?? ""
                })
                .AsNoTracking()
                .ToListAsync();

            decimal totalMontoSolicitadoEnCurso = solicitudesPeriodo.Sum(s => s.MontoSolicitado);
            decimal saldoDisponibleEfectivo = Math.Max(0m, topeMaximo - totalMontoSolicitadoEnCurso);

            return new ResumenSocioUtilidadDto
            {
                TotalUtilidadAcumulada = saldoConsolidado,
                PorcentajePermitido = pctPermitido,
                TopeMaximoRetiro = topeMaximo,
                SaldoDisponibleRetiro = saldoDisponibleEfectivo,
                MontoSolicitadoEnCurso = totalMontoSolicitadoEnCurso,
                DentroDeFechaVentanilla = enVentanilla,
                SocioHabilitado = true,
                MensajeInhabilitacion = string.Empty,
                NombrePeriodo = (periodo.NombrePeriodo ?? "UTILIDAD").Trim().ToUpper(),
                IdPeriodoConfig = periodo.IdPeriodoConfig,
                FechaApertura = fechaApertura,
                FechaCierre = fechaCierre,
                SolicitudesPrevias = solicitudesPeriodo.OrderByDescending(x => x.FechaSolicitud).ToList()
            };
        }

        public async Task<IEnumerable<UtilidadesProcesadas>> ObtenerDetalleMensualSocioAsync(int idSocio)
        {
            return await _context.UtilidadesProcesadas
                .Where(u => u.IdSocio == idSocio)
                .OrderBy(u => u.Anio).ThenBy(u => u.Mes)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task RegistrarSolicitudAsync(SolicitudUtilidadDto dto)
        {
            var resumen = await ObtenerResumenRetiroSocioAsync(dto.IdSocio);

            if (!resumen.SocioHabilitado)
                throw new InvalidOperationException(resumen.MensajeInhabilitacion);

            if (!resumen.DentroDeFechaVentanilla)
                throw new InvalidOperationException("La ventana de cobro de utilidades no se encuentra activa.");

            if (dto.MontoSolicitado <= 0 || dto.MontoSolicitado > (resumen.SaldoDisponibleRetiro + 0.05m))
                throw new InvalidOperationException($"El monto solicitado supera su límite disponible actual (Máx: S/ {resumen.SaldoDisponibleRetiro:N2}).");

            var solicitud = new SolicitudUtilidad
            {
                IdSocio = dto.IdSocio,
                IdPeriodoConfig = dto.IdPeriodoConfig,
                MontoSolicitado = dto.MontoSolicitado,
                TipoRetiro = dto.MontoSolicitado >= resumen.SaldoDisponibleRetiro ? "TOTAL" : "PARCIAL",
                Estado = "PENDIENTE",
                FechaSolicitud = DateTime.Now
            };

            _context.SolicitudesUtilidad.Add(solicitud);
            await _context.SaveChangesAsync();
        }

        public async Task ModificarSolicitudAsync(int idSolicitud, decimal nuevoMonto)
        {
            var solicitud = await _context.SolicitudesUtilidad.FindAsync(idSolicitud);
            if (solicitud == null || solicitud.Estado != "PENDIENTE")
                throw new InvalidOperationException("La solicitud no existe o ya fue procesada en caja.");

            var resumen = await ObtenerResumenRetiroSocioAsync(solicitud.IdSocio);
            decimal disponibleParaEdicion = resumen.SaldoDisponibleRetiro + solicitud.MontoSolicitado;

            if (nuevoMonto <= 0 || nuevoMonto > (disponibleParaEdicion + 0.05m))
                throw new InvalidOperationException($"El nuevo monto excede su margen disponible (Máx: S/ {disponibleParaEdicion:N2}).");

            solicitud.MontoSolicitado = nuevoMonto;
            solicitud.TipoRetiro = nuevoMonto >= disponibleParaEdicion ? "TOTAL" : "PARCIAL";
            solicitud.FechaSolicitud = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task EliminarSolicitudLogicAsync(int idSolicitud)
        {
            var solicitud = await _context.SolicitudesUtilidad.FindAsync(idSolicitud);
            if (solicitud == null) throw new KeyNotFoundException("Solicitud no encontrada.");

            if (solicitud.Estado != "PENDIENTE")
                throw new InvalidOperationException("Únicamente se pueden anular solicitudes en estado PENDIENTE.");

            solicitud.Estado = "ELIMINADO";
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<SolicitudUtilidad>> ListarSolicitudesPorEstadoAsync(string estado)
        {
            return await _context.SolicitudesUtilidad
                .Include(s => s.PeriodoConfig)
                .Where(s => s.Estado == estado)
                .AsNoTracking()
                .ToListAsync();
        }

        // 🎯 1. Implementación exacta requerida por ISolicitudUtilidadService
        public async Task<IEnumerable<SolicitudPendienteCajaDto>> ListarSolicitudesPendientesOrdenadasAsync()
        {
            var solicitudes = await (from sol in _context.SolicitudesUtilidad
                                     join soc in _context.Socios on sol.IdSocio equals soc.IdSocio
                                     join per in _context.PeriodosRetiroUtilidad on sol.IdPeriodoConfig equals per.IdPeriodoConfig
                                     where sol.Estado == "PENDIENTE"
                                     orderby soc.ApellidoPaterno, soc.ApellidoMaterno, soc.Nombres
                                     select new
                                     {
                                         sol.IdSolicitud,
                                         sol.IdSocio,
                                         SocioNombreCompleto = $"{soc.ApellidoPaterno} {soc.ApellidoMaterno} {soc.Nombres}".Trim().ToUpper(),
                                         sol.IdPeriodoConfig,
                                         NombrePeriodo = (per.NombrePeriodo ?? string.Empty).Trim().ToUpper(),
                                         sol.MontoSolicitado,
                                         sol.TipoRetiro,
                                         sol.FechaSolicitud,
                                         sol.Estado,
                                         PeriodoObj = per
                                     }).AsNoTracking().ToListAsync();

            var resultado = new List<SolicitudPendienteCajaDto>();

            foreach (var s in solicitudes)
            {
                var totalUtilidad = await _context.UtilidadesConsolidadas
                    .Where(c => c.IdSocio == s.IdSocio && c.IdPeriodoConfig == s.IdPeriodoConfig)
                    .Select(c => c.TotalUtilidadAcumulada > 0 ? c.TotalUtilidadAcumulada : c.SaldoUtilidad)
                    .FirstOrDefaultAsync();

                if (totalUtilidad <= 0)
                {
                    var utilidades = await _context.UtilidadesProcesadas
                        .Where(u => u.IdSocio == s.IdSocio && u.IdPeriodoConfig == s.IdPeriodoConfig)
                        .AsNoTracking()
                        .ToListAsync();

                    totalUtilidad = utilidades.Sum(u => (u.InteresMensualRepartir ?? u.UtilidadObtenida ?? u.MontoUtilidadGenerada ?? 0m));
                }

                decimal pctPermitido = 100.00m;
                var propPct = s.PeriodoObj.GetType().GetProperty("PorcentajeMaximoRetiro");
                if (propPct != null)
                {
                    var val = Convert.ToDecimal(propPct.GetValue(s.PeriodoObj, null) ?? 0m);
                    if (val > 0) pctPermitido = val;
                }

                decimal montoTope = Math.Round(totalUtilidad * (pctPermitido / 100m), 2);

                resultado.Add(new SolicitudPendienteCajaDto
                {
                    IdSolicitud = s.IdSolicitud,
                    IdSocio = s.IdSocio,
                    SocioNombreCompleto = s.SocioNombreCompleto,
                    IdPeriodoConfig = s.IdPeriodoConfig,
                    NombrePeriodo = s.NombrePeriodo,
                    MontoSolicitado = s.MontoSolicitado,
                    TotalUtilidad = totalUtilidad,
                    MontoTope = montoTope,
                    TipoRetiro = s.TipoRetiro ?? "PARCIAL",
                    FechaSolicitud = Convert.ToDateTime(s.FechaSolicitud).Date,
                    Estado = s.Estado
                });
            }

            return resultado;
        }

        // 🎯 2. Procesar Desembolso sin duplicados y con conversión segura de fecha (DateTime? / string)
        public async Task ProcesarDesembolsoCajaAsync(DesembolsoPayloadDto desembolso)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Cargar la solicitud en estado PENDIENTE
                    var solicitud = await _context.SolicitudesUtilidad.FindAsync(desembolso.IdSolicitud);
                    if (solicitud == null || solicitud.Estado != "PENDIENTE")
                        throw new InvalidOperationException("La solicitud no existe o no se encuentra en estado PENDIENTE.");

                    decimal totalValidado = desembolso.MediosPago?.Sum(m => m.Monto) ?? 0m;
                    if (Math.Abs(totalValidado - solicitud.MontoSolicitado) > 0.05m)
                        throw new InvalidOperationException("El desglose por medio de pago no coincide exactamente con el monto solicitado.");

                    // 🎯 Parseo seguro de fecha
                    DateTime fechaOperacion = DateTime.Now;
                    var propFecha = desembolso.GetType().GetProperty("FechaDesembolso");
                    if (propFecha != null)
                    {
                        var valFecha = propFecha.GetValue(desembolso, null);
                        if (valFecha is DateTime dtVal)
                        {
                            fechaOperacion = dtVal;
                        }
                        else if (valFecha is string strVal && !string.IsNullOrWhiteSpace(strVal))
                        {
                            if (DateTime.TryParse(strVal, out DateTime dtParsed))
                            {
                                fechaOperacion = dtParsed;
                            }
                        }
                    }

                    int idUsuarioValido = desembolso.IdUsuarioCaja > 0 ? desembolso.IdUsuarioCaja : 1;

                    // 2. Actualizar estado de la Solicitud
                    solicitud.Estado = "DESEMBOLSADO";
                    solicitud.ComentarioCaja = string.IsNullOrWhiteSpace(desembolso.Comentario)
                        ? $"Desembolso de utilidad folio #{solicitud.IdSolicitud}"
                        : desembolso.Comentario.Trim();
                    solicitud.FechaProcesadoCaja = fechaOperacion;
                    solicitud.IdUsuarioCaja = idUsuarioValido;

                    // 3. Registrar Detalle de Desembolso y Movimiento de Caja
                    foreach (var mp in desembolso.MediosPago)
                    {
                        var detalle = new SolicitudUtilidadDesembolsoDetalle
                        {
                            IdSolicitud = solicitud.IdSolicitud,
                            IdMedioPago = mp.IdMedioPago,
                            MontoDesembolsado = mp.Monto,
                            ReferenciaOperacion = string.IsNullOrWhiteSpace(mp.Referencia) ? $"PAGO UTILIDAD #{solicitud.IdSolicitud}" : mp.Referencia.Trim(),
                            FechaDesembolso = fechaOperacion
                        };
                        _context.SolicitudUtilidadDesembolsoDetalles.Add(detalle);

                        // 🎯 Movimiento de Caja asignado con IdConcepto = 7 ("PAGO DE UTILIDAD")
                        var movCaja = new MovimientoCaja
                        {
                            IdConcepto = 7, // PAGO DE UTILIDAD (Egreso 'E')
                            IdCredito = solicitud.IdSolicitud,
                            Monto = mp.Monto,
                            FechaMovimiento = fechaOperacion,
                            Fecha = fechaOperacion,
                            IdUsuario = idUsuarioValido,
                            Estado = "ACTIVO",
                            IdAsiento = null,
                            IdCaja = 1,
                            IdMedioPago = mp.IdMedioPago > 0 ? mp.IdMedioPago : null,
                            Concepto = null // Se pasa nulo en la creación para evitar el DataReader error
                        };

                        _context.MovimientosCaja.Add(movCaja);
                    }

                    // 4. Descontar del Saldo Consolidado del Socio
                    var saldoMaestro = await _context.UtilidadesConsolidadas
                        .FirstOrDefaultAsync(c => c.IdSocio == solicitud.IdSocio && c.IdPeriodoConfig == solicitud.IdPeriodoConfig);

                    if (saldoMaestro != null)
                    {
                        saldoMaestro.SaldoUtilidad = Math.Max(0m, saldoMaestro.SaldoUtilidad - totalValidado);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
        public async Task RechazarSolicitudCajaAsync(int idSolicitud, int idUsuario, string comentario)
        {
            var solicitud = await _context.SolicitudesUtilidad.FindAsync(idSolicitud);
            if (solicitud == null) throw new KeyNotFoundException("Solicitud no encontrada.");

            solicitud.Estado = "RECHAZADO";
            solicitud.ComentarioCaja = comentario;
            solicitud.FechaProcesadoCaja = DateTime.Now;
            solicitud.IdUsuarioCaja = idUsuario;

            await _context.SaveChangesAsync();
        }
    }
}
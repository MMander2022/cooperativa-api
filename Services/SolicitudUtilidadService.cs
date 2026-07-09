using CooperativaApp.Data;
using CooperativaApp.DTOs;
using CooperativaApp.DTOS;
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
            var fechaActual = DateTime.Now;

            // Buscar periodo activo en fase de retiro
            var periodo = await _context.PeriodosRetiroUtilidad
                .FirstOrDefaultAsync(p => p.Estado == "PROCESADO" || p.Estado == "HABILITADO");

            if (periodo == null) return new ResumenSocioUtilidadDto { DentroDeFechaVentanilla = false };

            bool enVentanilla = fechaActual >= periodo.FechaAperturaRetiro && fechaActual <= periodo.FechaCierreRetiro;

            var saldoConsolidado = await _context.UtilidadesConsolidadas
             .Where(c => c.IdSocio == idSocio && c.IdPeriodoConfig == periodo.IdPeriodoConfig)
             .Select(c => c.SaldoUtilidad)
             .FirstOrDefaultAsync();

            return new ResumenSocioUtilidadDto
            {
                SaldoDisponible = saldoConsolidado,
                DentroDeFechaVentanilla = enVentanilla,
                NombrePeriodo = periodo.NombrePeriodo
            };
        }

        public async Task<IEnumerable<UtilidadesProcesadas>> ObtenerDetalleMensualSocioAsync(int idSocio)
        {
            return await _context.UtilidadesProcesadas
                .Where(u => u.IdSocio == idSocio)
                .OrderBy(u => u.Anio).ThenBy(u => u.Mes)
                .ToListAsync();
        }

        public async Task RegistrarSolicitudAsync(SolicitudUtilidadDto dto)
        {
            var solicitud = new SolicitudUtilidad
            {
                IdSocio = dto.IdSocio,
                IdPeriodoConfig = dto.IdPeriodoConfig,
                MontoSolicitado = dto.MontoSolicitado,
                TipoRetiro = dto.TipoRetiro,
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
                throw new InvalidOperationException("La solicitud no existe o ya fue procesada.");

            solicitud.MontoSolicitado = nuevoMonto;
            solicitud.TipoRetiro = "PARCIAL"; // Se autoajusta recursivamente
            await _context.SaveChangesAsync();
        }

        public async Task EliminarSolicitudLogicAsync(int idSolicitud)
        {
            var solicitud = await _context.SolicitudesUtilidad.FindAsync(idSolicitud);
            if (solicitud == null) throw new KeyNotFoundException();

            solicitud.Estado = "ELIMINADO";
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<SolicitudUtilidad>> ListarSolicitudesPorEstadoAsync(string estado)
        {
            return await _context.SolicitudesUtilidad
                .Include(s => s.PeriodoConfig)
                .Where(s => s.Estado == estado)
                .ToListAsync();
        }

        public async Task ProcesarDesembolsoCajaAsync(DesembolsoPayloadDto desembolso)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var solicitud = await _context.SolicitudesUtilidad.FindAsync(desembolso.IdSolicitud);
                    if (solicitud == null || solicitud.Estado != "PENDIENTE")
                        throw new Exception("Solicitud no válida.");

                    decimal totalValidado = desembolso.MediosPago.Sum(m => m.Monto);
                    if (totalValidado != solicitud.MontoSolicitado)
                        throw new Exception("El desglose de medios de pago no cuadra con el monto solicitado.");

                    // 1. Cambiar estado de solicitud
                    solicitud.Estado = "APROBADO";
                    solicitud.ComentarioCaja = desembolso.Comentario;
                    solicitud.FechaProcesadoCaja = DateTime.Now;
                    solicitud.IdUsuarioCaja = desembolso.IdUsuarioCaja;

                    // 2. Registrar desglose por medio de pago
                    foreach (var mp in desembolso.MediosPago)
                    {
                        var detalle = new SolicitudUtilidadDesembolsoDetalle
                        {
                            IdSolicitud = solicitud.IdSolicitud,
                            IdMedioPago = mp.IdMedioPago,
                            MontoDesembolsado = mp.Monto,
                            ReferenciaOperacion = mp.Referencia
                        };
                        _context.SolicitudUtilidadDesembolsoDetalles.Add(detalle);

                        // 3. Impactar en MovimientosCaja (Tu infraestructura de libro diario)
                        var movCaja = new MovimientoCaja
                        {
                            IdConcepto = 7,
                            Monto = mp.Monto,
                            Fecha = DateTime.Now,
                            IdMedioPago = mp.IdMedioPago
                        };
                        _context.MovimientosCaja.Add(movCaja);
                    }

                    // 4. Debitar del Saldo Maestro Consolidado del Socio
                    var saldoMaestro = await _context.UtilidadesConsolidadas
                        .FirstOrDefaultAsync(c => c.IdSocio == solicitud.IdSocio && c.IdPeriodoConfig == solicitud.IdPeriodoConfig);

                    if (saldoMaestro != null)
                    {
                        saldoMaestro.SaldoUtilidad -= totalValidado;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task RechazarSolicitudCajaAsync(int idSolicitud, int idUsuario, string comentario)
        {
            var solicitud = await _context.SolicitudesUtilidad.FindAsync(idSolicitud);
            if (solicitud == null) throw new KeyNotFoundException();

            solicitud.Estado = "RECHAZADO";
            solicitud.ComentarioCaja = comentario;
            solicitud.FechaProcesadoCaja = DateTime.Now;
            solicitud.IdUsuarioCaja = idUsuario;

            await _context.SaveChangesAsync();
        }
    }
}
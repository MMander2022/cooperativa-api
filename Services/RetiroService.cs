using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CooperativaApp.Data;
using CooperativaApp.DTOs;
using CooperativaApp.Models;

namespace CooperativaApp.Services
{
    public class RetiroService : IRetiroService
    {
        private readonly CooperativaContext _context;

        public RetiroService(CooperativaContext context)
        {
            _context = context;
        }

        public async Task<PeriodoRetiro> ObtenerPeriodoActivoVentanillaAsync()
        {
            var hoy = DateTime.Today;
            // ── 🎯 EVALUACIÓN PARAMÉTRICA DIARIA POR MES Y AÑO ──
            return await _context.PeriodosRetiro.FirstOrDefaultAsync(p => p.Activo && p.MesPermitido == hoy.Month && p.AnioFiscal == hoy.Year);
        }
        public async Task<List<RetiroItemResponse>> ListarMisSolicitudesAsync(int idSocio)
        {
            // ── 🎯 ARQUITECTURA DINÁMICA: Join con Socios y comodín para ID 0 (Consolidado Global) ──
            var querySaneada = from sol in _context.SolicitudesRetiro
                               join per in _context.PeriodosRetiro on sol.IdPeriodo equals per.IdPeriodo
                               join soc in _context.Socios on sol.IdSocio equals soc.IdSocio // 👥 Join dinámico para rescatar nombres
                                                                                             // ✅ CONDICIONAL CORE: Si idSocio es 0, no filtra y expone el núcleo completo
                               where (idSocio == 0 || sol.IdSocio == idSocio)
                               orderby sol.FechaSolicitud descending
                               select new RetiroItemResponse
                               {
                                   IdSolicitud = sol.IdSolicitud,
                                   IdSocio = sol.IdSocio,
                                   // ✅ ASIGNACIÓN REAL: Ya no más strings vacíos, viaja el nombre oficial de la BD
                                   SocioNombre = soc.Nombres.ToUpper().Trim(),
                                   PeriodoNombre = per.NombrePeriodo,
                                   Monto = sol.MontoSolicitado,
                                   // Parseo nativo que el Front ya sabe interpretar con new Date()
                                   Fecha = sol.FechaSolicitud.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                                   Estado = sol.Estado,
                                   Motivo = sol.MotivoRechazo ?? ""
                               };

            return await querySaneada.AsNoTracking().ToListAsync();
        }

        public async Task<List<RetiroItemResponse>> ListarPendientesCajaAsync()
        {
            // ── 🎯 BLINDAJE EN COLA DE TESORERÍA: Triple Join Explícito sin Includes virtuales ──
            var queryCaja = from sol in _context.SolicitudesRetiro
                            join per in _context.PeriodosRetiro on sol.IdPeriodo equals per.IdPeriodo
                            join soc in _context.Socios on sol.IdSocio equals soc.IdSocio // Ajustar a .Socios si tu DbSet usa plural
                            where sol.Estado == "PENDIENTE"
                            orderby sol.FechaSolicitud ascending
                            select new RetiroItemResponse
                            {
                                IdSolicitud = sol.IdSolicitud,
                                IdSocio = sol.IdSocio,
                                SocioNombre = (soc.Nombres + " " + soc.ApellidoPaterno + " " + soc.ApellidoMaterno).Trim().ToUpper(),
                                PeriodoNombre = per.NombrePeriodo,
                                Monto = sol.MontoSolicitado,
                                Fecha = sol.FechaSolicitud.ToString("dd/MM/yyyy HH:mm"),
                                Estado = sol.Estado,
                                Motivo = ""
                            };

            return await queryCaja.AsNoTracking().ToListAsync();
        }

        public async Task<(bool Success, string Message)> RegistrarSolicitudAsync(int idSocio, SolicitudRetiroDto dto)
        {
            var socio = await _context.Socios.FindAsync(idSocio); // Ajustar a _context.Socios si aplica
            if (socio == null || !socio.PermiteRetiro.GetValueOrDefault())
                return (false, "El socio no cuenta con la habilitación legal para procesar retiros en este periodo.");

            var periodo = await ObtenerPeriodoActivoVentanillaAsync();
            if (periodo == null)
                return (false, "No existen ventanas de retiro autorizadas abiertas para el mes y año en curso.");

            // ── 🎯 CORRECCIÓN: Cambiado 'p.IdPeriodo' por 'periodo.IdPeriodo' ──
            bool yaTieneRegistro = await _context.SolicitudesRetiro
                .AnyAsync(s => s.IdSocio == idSocio && s.IdPeriodo == periodo.IdPeriodo && s.Estado != "RECHAZADO");

            if (yaTieneRegistro)
                return (false, "Ya cuenta con una solicitud registrada o aprobada para el periodo vigente.");

            var nuevaSolicitud = new SolicitudRetiro
            {
                IdSocio = idSocio,
                IdPeriodo = periodo.IdPeriodo,
                MontoSolicitado = dto.MontoSolicitado,
                FechaSolicitud = DateTime.Now,
                Estado = "PENDIENTE",
                Socio = null,
                PeriodoRetiro = null
            };

            _context.SolicitudesRetiro.Add(nuevaSolicitud);
            await _context.SaveChangesAsync();
            return (true, "Solicitud de retiro registrada. Pendiente de aprobación de Caja.");
        }

        public async Task<(bool Success, string Message)> ModificarSolicitudAsync(int idSolicitud, int idSocio, decimal nuevoMonto)
        {
            // Buscamos la solicitud en la base de datos de forma limpia
            var solicitud = await _context.SolicitudesRetiro.FindAsync(idSolicitud);

            if (solicitud == null || solicitud.IdSocio != idSocio)
                return (false, "Registro de retiro no encontrado.");

            if (solicitud.Estado == "APROBADO")
                return (false, "Operación denegada. Las solicitudes aprobadas por caja no permiten modificaciones.");

            // Aplicamos el cambio de estado operativo
            solicitud.MontoSolicitado = nuevoMonto;
            solicitud.Estado = "PENDIENTE";
            solicitud.MotivoRechazo = null; // Al editarse, se limpia el motivo de rechazo anterior si lo hubiera
            solicitud.FechaSolicitud = DateTime.Now;

            await _context.SaveChangesAsync();
            return (true, "Solicitud modificada con éxito. Re-enviada a Caja.");
        }
        public async Task<(bool Success, string Message)> AnularSolicitudAsync(int idSolicitud, int idSocio)
        {
            // ── 🎯 CONTROL DE LECTURA INMUNE: Jala el registro interpretando el string? sin excepciones ──
            var solicitud = await _context.SolicitudesRetiro.FindAsync(idSolicitud);

            if (solicitud == null || solicitud.IdSocio != idSocio)
                return (false, "Registro de retiro no encontrado.");

            if (solicitud.Estado == "APROBADO")
                return (false, "Operación denegada. No se puede anular una solicitud que ya cuenta con desembolso de caja.");

            // ── 🎯 REGLA DE ESTADOS CONTABLES DE UNIMAS ──
            solicitud.Estado = "RECHAZADO";
            // Dejamos un log limpio en el campo nullable para auditoría de los stakeholders
            solicitud.MotivoRechazo = "ANULADO VOLUNTARIAMENTE POR EL SOCIO";
            solicitud.FechaSolicitud = DateTime.Now;

            await _context.SaveChangesAsync(); // 👈 Persiste de inmediato al centavo
            return (true, "Solicitud anulada con éxito. Ventana de periodo liberada para el socio.");
        }
        public async Task<(bool Success, string Message)> EvaluarSolicitudCajaAsync(EvaluacionRetiroDto dto)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var sol = await _context.SolicitudesRetiro.FindAsync(dto.IdSolicitud);
                    if (sol == null || sol.Estado != "PENDIENTE")
                        return (false, "La solicitud no se encuentra en estado PENDIENTE para evaluación.");

                    sol.Estado = dto.Estado.ToUpper().Trim();
                    sol.IdUsuarioAuditoria = dto.IdUsuario;
                    sol.FechaAuditoria = DateTime.Now;

                    if (sol.Estado == "RECHAZADO")
                    {
                        sol.MotivoRechazo = dto.MotivoRechazo?.ToUpper() ?? "RECHAZADO POR CAJA CENTRAL";
                    }
                    else if (sol.Estado == "APROBADO")
                    {
                        if (dto.IdMedioPago == null) return (false, "Debe especificar el medio de pago para liquidar la caja.");

                        // ── 🎯 CORRECCIÓN: Cambiado 'FechaMovimiento' por el campo nativo 'Fecha' de tu BD ──
                        var movimientoCaja = new MovimientoCaja
                        {
                            IdConcepto = 6,
                            IdCredito = sol.IdSocio,
                            Monto = sol.MontoSolicitado,
                            Fecha = DateTime.Now, // 👈 Se acopla a tu campo contable real
                            IdUsuario = dto.IdUsuario,
                            Estado = "PROCESADO",
                            IdCaja = 1,
                            IdMedioPago = dto.IdMedioPago.Value
                        };

                        _context.MovimientosCaja.Add(movimientoCaja);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return (true, $"Solicitud procesada como {sol.Estado} con éxito.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return (false, $"Fallo en la transacción: {ex.Message}");
                }
            }
        }
    }
}
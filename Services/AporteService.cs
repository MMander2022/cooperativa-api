using CooperativaApp.Data;
using CooperativaApp.Interfaces;
using CooperativaApp.Models;
using Microsoft.EntityFrameworkCore; 

namespace CooperativaApp.Services
{

    public class AporteService : IAporteService
    {
        private readonly CooperativaContext _context;

        public AporteService(CooperativaContext context)
        {
            _context = context;
        }

        public async Task<ConfigAporte> GetConfiguracionVigenteAsync()
        {
            return await _context.ConfigAportes
                .Where(c => c.Estado && (c.FechaFin == null || c.FechaFin >= DateTime.Now))
                .OrderByDescending(c => c.FechaInicio)
                .FirstOrDefaultAsync() ?? throw new Exception("No hay configuración de aportes vigente.");
        }
        public async Task<(bool Success, string Message)> RegistrarAporteAsync(AporteSocio aporte)
        {
            try
            {
                // 🛡️ VALIDACIÓN TITANIUM: 
                // Solo bloqueamos si el socio tiene un aporte que ya está en manos de la cooperativa (P o A).
                // Si el aporte anterior fue 'E' (Eliminado) o 'R' (Rechazado), el socio tiene derecho a intentar de nuevo.
                bool existeVigente = await _context.AportesSocios
                    .AnyAsync(a => a.IdSocio == aporte.IdSocio &&
                                   a.MesAportado == aporte.MesAportado &&
                                   a.AnioAportado == aporte.AnioAportado &&
                                   (a.EstadoPago == 'P' || a.EstadoPago == 'A'));

                if (existeVigente)
                {
                    return (false, $"Ya tienes un aporte vigente o aprobado para el periodo {aporte.MesAportado}/{aporte.AnioAportado}.");
                }

                // ⚙️ PROCESAMIENTO DE PARÁMETROS
                var config = await GetConfiguracionVigenteAsync();

                aporte.IdConfig = config.IdConfig;
                aporte.MontoPagado = aporte.CantidadAcciones * config.ValorAccion;
                aporte.FechaPago = DateTime.Now;
                aporte.EstadoPago = 'P';

                // 💎 LIMPIEZA DE NAVEGACIÓN (Evita conflictos de EF Core)
                aporte.Socio = null!;
                aporte.ConfigAporte = null!;

                // 🚀 INSERCIÓN DE NUEVA FILA (Preservando el historial)
                // Al ser un nuevo objeto AporteSocio, generará un nuevo IdAporte.
                _context.AportesSocios.Add(aporte);
                _context.Entry(aporte).State = EntityState.Added;

                await _context.SaveChangesAsync();

                return (true, "¡Misión cumplida! Tu nuevo aporte ha sido registrado para revisión.");
            }
            catch (DbUpdateException ex)
            {
                // 🕵️ CASO DE FALLA POR LLAVE ÚNICA (Unique Constraint en DB)
                var sqlMsg = ex.InnerException?.Message ?? ex.Message;
                if (sqlMsg.Contains("Unique") || sqlMsg.Contains("UQ"))
                {
                    return (false, "La base de datos tiene una restricción de unicidad. Contacte a soporte técnico para revisar los índices de la tabla.");
                }
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en DB: {ex.Message}");
                return (false, "Inconveniente técnico al procesar el registro. Intente nuevamente.");
            }
        }
        public async Task<decimal> GetTotalAcumuladoAnualAsync(int idSocio, int anio)
        {
            return await _context.AportesSocios
                .Where(a => a.IdSocio == idSocio && a.AnioAportado == anio)
                .SumAsync(a => a.MontoPagado);
        }
        public async Task<(bool Success, string Message)> ValidarAporteCajaAsync(int idAporte, char nuevoEstado, int idCajero, string? comentario)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var aporte = await _context.AportesSocios.Include(a => a.Socio).FirstOrDefaultAsync(a => a.IdAporte == idAporte);
                if (aporte == null || aporte.EstadoPago != 'P') return (false, "Aporte no válido para procesar.");

                if (nuevoEstado == 'A')
                {
                    // 1. Generar Asiento Contable
                    var asiento = new AsientosContables
                    {
                        Glosa = $"Aporte mensual {aporte.MesAportado}/{aporte.AnioAportado} - Socio: {aporte.Socio.Nombres}",
                        IdReferencia = aporte.IdAporte,
                        Origen = "APORTES",
                        Fecha = DateTime.Now
                    };
                    _context.AsientosContables.Add(asiento);
                    await _context.SaveChangesAsync();

                    // 2. Generar Movimiento de Caja (Usando tu tabla)
                    var movimiento = new MovimientoCaja
                    {
                        IdConcepto = 1, // ID definido para Aportes
                        Monto = aporte.MontoPagado,
                        IdUsuario = idCajero,
                        Estado = "ACTIVO",
                        Fecha = DateTime.Now,
                      
                        IdAsiento = asiento.IdAsiento, // Vínculo contable
                        IdCaja = 1 // Caja principal
                    };
                    _context.MovimientosCaja.Add(movimiento);
                    await _context.SaveChangesAsync();

                    aporte.IdMovimientoCaja = movimiento.IdMovimiento;
                }

                aporte.EstadoPago = nuevoEstado;
                aporte.FechaValidacion = DateTime.Now;
                aporte.IdUsuarioValidador = idCajero;
                aporte.ComentarioCaja = comentario;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return (true, nuevoEstado == 'A' ? "Pago conciliado y contabilizado." : "Pago rechazado.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Fallo en el núcleo: {ex.Message}");
            }
        }
        public async Task<(bool Success, string Message)> RegistrarIntencionAporteAsync(AporteSocio aporte)
        {
            // 1. Obtener la configuración de acciones vigente
            var config = await _context.ConfigAportes
                .Where(c => c.Estado && (c.FechaFin == null || c.FechaFin >= DateTime.Now))
                .OrderByDescending(c => c.FechaInicio)
                .FirstOrDefaultAsync();

            if (config == null) return (false, "No hay una configuración de acciones activa.");

            // 2. Cálculo Titanium: Cantidad x Valor Nominal
            aporte.IdConfig = config.IdConfig;
            aporte.MontoPagado = aporte.CantidadAcciones * config.ValorAccion;
            aporte.EstadoPago = 'P'; // Pendiente de validación por caja
            aporte.FechaPago = DateTime.Now;

            _context.AportesSocios.Add(aporte);
            await _context.SaveChangesAsync();

            return (true, $"Aporte de {aporte.CantidadAcciones} acciones registrado por S/ {aporte.MontoPagado}.");
        }
        // En AporteService.cs
        public async Task<(bool Success, string Message)> CambiarValorAccionAsync(decimal nuevoMonto, int idUsuario)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Finalizar la vigencia actual
                var vigente = await _context.ConfigAportes
                    .FirstOrDefaultAsync(c => c.Estado && c.FechaFin == null);

                if (vigente != null)
                {
                    vigente.FechaFin = DateTime.Now;
                    vigente.Estado = false;
                }

                // 2. Insertar la nueva regla Titanium
                var nuevaConfig = new ConfigAporte
                {
                    ValorAccion = nuevoMonto, // o Monto, según tu tabla
                    FechaInicio = DateTime.Now,
                    Estado = true,
                    IdUsuarioRegistro = idUsuario,
                    FechaRegistro = DateTime.Now
                };

                _context.ConfigAportes.Add(nuevaConfig);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Valor de acción actualizado globalmente.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, ex.Message);
            }
        }

    }
}

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
    public class CajaService : ICajaService
    {
        private readonly CooperativaContext _context;

        public CajaService(CooperativaContext context)
        {
            _context = context;
        }

        public async Task<CuadreDiarioDto> ObtenerCuadreDiarioAsync(DateTime fecha)
        {
            var rawData = await _context.Set<CuadreCajaSpResponse>()
                .FromSqlInterpolated($"EXEC dbo.sp_ObtenerCuadreDiarioCaja {fecha.ToString("yyyy-MM-dd")}")
                .ToListAsync();

            // Filtrado matemático estricto segregando Ingresos (I) vs Egresos (E)
            decimal ingresos = rawData.Where(x => x.TipoMovimiento == "I" && x.Estado == "PROCESADO").Sum(x => x.Monto);
            decimal egresos = rawData.Where(x => x.TipoMovimiento == "E").Sum(x => x.Monto); // Desembolsos restan caja de inmediato

            return new CuadreDiarioDto
            {
                FechaCuadre = fecha.ToString("dd/MM/yyyy"),
                TotalIngresos = ingresos,
                TotalEgresos = egresos,
                SaldoNetoDelDia = ingresos - egresos,
                TotalTransacciones = rawData.Count,
                Movimientos = rawData.Select(m => new MovimientoCajaDetalleDto
                {
                    IdMovimiento = m.IdMovimiento,
                    Monto = m.Monto,
                    Hora = m.FechaMovimiento.ToString("HH:mm:ss"),
                    Estado = m.Estado,
                    Concepto = m.ConceptoNombre.ToUpper(),
                    Tipo = m.TipoMovimiento,
                    CuentaDebe = m.CuentaContableDebe ?? "-",
                    CuentaHaber = m.CuentaContableHaber ?? "-",
                    Beneficiario = m.BeneficiarioNombre,
                    Dni = m.BeneficiarioDni,
                    MedioPago = m.MedioPagoDescripcion
                }).ToList()
            };
        }
    }
}
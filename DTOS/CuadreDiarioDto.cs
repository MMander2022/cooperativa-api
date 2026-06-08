using System.Collections.Generic;

namespace CooperativaApp.DTOs
{
    public class CuadreDiarioDto
    {
        public string FechaCuadre { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal TotalEgresos { get; set; }
        public decimal SaldoNetoDelDia { get; set; }
        public int TotalTransacciones { get; set; }
        public List<MovimientoCajaDetalleDto> Movimientos { get; set; }
    }

    public class MovimientoCajaDetalleDto
    {
        public int IdMovimiento { get; set; }
        public decimal Monto { get; set; }
        public string Hora { get; set; }
        public string Estado { get; set; }
        public string Concepto { get; set; }
        public string Tipo { get; set; } // "I" o "E"
        public string CuentaDebe { get; set; }
        public string CuentaHaber { get; set; }
        public string Beneficiario { get; set; }
        public string Dni { get; set; }
        public string MedioPago { get; set; }
    }
}
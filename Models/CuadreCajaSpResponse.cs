using System;

namespace CooperativaApp.Models
{
    public class CuadreCajaSpResponse
    {
        public int IdMovimiento { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public string Estado { get; set; }
        public int IdConcepto { get; set; }
        public string ConceptoNombre { get; set; }
        public string TipoMovimiento { get; set; }
        public string CuentaContableDebe { get; set; }
        public string CuentaContableHaber { get; set; }
        public int? IdCredito { get; set; }
        public string BeneficiarioNombre { get; set; }
        public string BeneficiarioDni { get; set; }
        public int? IdMedioPago { get; set; }
        public string MedioPagoDescripcion { get; set; }
    }
}
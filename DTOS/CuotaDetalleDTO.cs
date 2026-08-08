namespace CooperativaApp.DTOS
{
    public class CuotaDetalleDTO
    {
        public int NumeroCuota { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public DateTime? FechaSolicitud { get; set; }
        public decimal MontoCuota { get; set; }
        public string Estado { get; set; }
        public DateTime? FechaPago { get; set; }
        public decimal? SaldoCapital { get; set; }
        public decimal? SaldoInteres { get; set; }
        public decimal? SaldoMora { get; set; }

        // 🎯 SALDOS FÍSICOS
        public decimal SaldoCuota { get; set; } // Lo que falta pagar (SaldoCapital + SaldoInteres)
        // 💎 CONSTRUCTOR DIAMANTE: Permite la creación rápida desde el bucle for

        public decimal MontoPagadoReal { get; set; }
        public string? MedioPagoReal { get; set; }

        // ⏳ INFO EN REVISIÓN (Solicitudes Pendientes de Socios)
        public decimal MontoEnRevision { get; set; }
        public string? MedioRevision { get; set; }
        /* public CuotaDetalleDTO(int numeroCuota, DateTime fechaVencimiento, decimal montoCuota, string estado, decimal saldoCuota,
             decimal? saldoCapital, DateTime? fechaPago=null, decimal montoPagadoReal = 0,
             decimal montoEnRevision = 0,
             string? medioRevision = null, string? medioPagoReal = null, DateTime? fechaSolicitud = null)
         {
             NumeroCuota = numeroCuota;
             FechaVencimiento = fechaVencimiento;
             MontoCuota = montoCuota;
             Estado = estado;
             SaldoCuota = saldoCuota;
             SaldoCapital = saldoCapital;
             FechaPago = fechaPago;
             MontoPagadoReal = montoPagadoReal;
             MontoEnRevision = montoEnRevision;
             MedioRevision = medioRevision;
             MedioPagoReal = medioPagoReal;
             FechaSolicitud = fechaSolicitud;
         }*/

        // Requerido para serialización
        public CuotaDetalleDTO() { }
    }
}
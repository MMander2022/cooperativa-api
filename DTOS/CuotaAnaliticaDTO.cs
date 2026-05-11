namespace CooperativaApp.DTOS
{
    public class CuotaAnaliticaDTO
    {
        public int NumeroCuota { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public string Estado { get; set; }
        public decimal MontoCuota { get; set; } // Total pactado

        // --- PACTADO (Valores estáticos de la cuota) ---
        public decimal Capital { get; set; }
        public decimal Interes { get; set; }

        // --- SALDOS (Deuda viva - Trifecta) ---
        public decimal SaldoCapital { get; set; }
        public decimal SaldoInteres { get; set; }
        public decimal SaldoMora { get; set; }
        public decimal SaldoTotal { get; set; }

        // --- PAGOS (Realidad técnica de la tabla pagosdetalle) ---
        public decimal PagoCapital { get; set; }
        public decimal PagoInteres { get; set; }
        public decimal PagoMora { get; set; }
        public decimal TotalPagado { get; set; }

        // --- AUDITORÍA ---
        public DateTime? FechaUltimoPago { get; set; }
        public string? MedioPago { get; set; }
        public decimal MontoEnRevision { get; set; }

        public CuotaAnaliticaDTO() { }
    }
}

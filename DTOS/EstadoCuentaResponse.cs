namespace CooperativaApp.DTOs
{
    public class EstadoCuentaResponse
    {
        public int IdCredito { get; set; }
        public decimal MontoOriginal { get; set; }
        public decimal SaldoCapitalActual { get; set; }

        public decimal TotalCapitalPagado { get; set; }
        public decimal TotalInteresPagado { get; set; }
        public decimal TotalMoraPagada { get; set; }

        public decimal TotalPendiente { get; set; }

        public int CuotasPagadas { get; set; }
        public int CuotasPendientes { get; set; }

        public int CuotasVencidas { get; set; }

        public decimal MoraAcumulada { get; set; }

        public string? EstadoCredito { get; set; }
    }
}

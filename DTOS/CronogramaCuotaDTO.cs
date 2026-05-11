namespace CooperativaApp.DTOS
{
    public class CronogramaCuotaDTO
    {
        public int NumeroCuota { get; set; }
        public DateTime FechaVencimiento { get; set; }

        public decimal CapitalProgramado { get; set; }
        public decimal InteresProgramado { get; set; }
        public decimal CuotaProgramada { get; set; }

        public decimal SaldoCapitalPendiente { get; set; }
        public decimal SaldoInteresPendiente { get; set; }
        public decimal SaldoCuotaPendiente { get; set; }

        public string Estado { get; set; } = "";
    }
}

namespace CooperativaApp.DTOS
{
    public class DetalleCreditoRiesgoDto
    {
        public int IdCredito { get; set; }
        public string Producto { get; set; } = string.Empty;
        public decimal MontoOtorgado { get; set; }
        public int PlazoMeses { get; set; }
        public int CuotasTotales { get; set; }
        public int CuotasPagadas { get; set; }
        public int CuotasConMora { get; set; }
        public decimal SaldoCapital { get; set; }
        public decimal SaldoInteres { get; set; }
        public decimal SaldoMora { get; set; }
        public decimal CapitalPagado { get; set; }
        public decimal InteresPagado { get; set; }
        public decimal MoraPagada { get; set; }
        public string EstadoCredito { get; set; } = string.Empty; // VIGENTE, CANCELADO, MOROSO
        public DateTime FechaDesembolso { get; set; }
    }
}

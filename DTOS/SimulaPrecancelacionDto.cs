namespace CooperativaApp.DTOS
{
    public class SimulaPrecancelacionDto
    {
        public int IdCredito { get; set; }
        public decimal SaldoMora { get; set; }
        public decimal SaldoInteres { get; set; }
        public decimal InteresProximaCuota { get; set; }
        public decimal SaldoCapital { get; set; }
        public decimal MontoTotalPrecancelacion { get; set; }
    }
}

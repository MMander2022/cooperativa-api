namespace CooperativaApp.DTOS
{
    public class KardexDTO
    {
        public DateTime Fecha { get; set; }
        public string TipoOperacion { get; set; } = "";

        public decimal Debe { get; set; }
        public decimal Haber { get; set; }

        public decimal SaldoCapital { get; set; }
    }
}

namespace CooperativaApp.DTOS
{
    public class CreditoSocioDTO
    {
        public int IdCredito { get; set; }
        public decimal MontoOriginal { get; set; }
        public string NombreSocio { get; set; }
        public DateTime? ProximoVencimiento { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Estado { get; set; }
        public DateTime? FechaUltimoDesembolso { get; set; }
    }
}

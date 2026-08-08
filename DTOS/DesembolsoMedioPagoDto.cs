namespace CooperativaApp.DTOS
{
    public class DesembolsoMedioPagoDto
    {
        public int IdMedioPago { get; set; }
        public decimal Monto { get; set; }
        public string Referencia { get; set; } = string.Empty;
    }
}

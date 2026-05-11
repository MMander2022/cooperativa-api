namespace CooperativaApp.DTOS
{
    public class SolicitudPagoDTO
    {
        public int IdCredito { get; set; }
        public decimal Monto { get; set; }
        public string MedioPago { get; set; }
        public string Referencia { get; set; }
        public string? ComprobanteUrl { get; set; }
    }
}

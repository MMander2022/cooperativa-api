namespace CooperativaApp.DTOS
{
    public class RegistrarSolicitudPagoDTO
    {
        public int IdCredito { get; set; }
        public decimal Monto { get; set; }
        public string MedioPago { get; set; } // 'YAPE', 'PLIN', 'TRANSFERENCIA', 'DEPOSITO'
        public string Referencia { get; set; } // Número de operación
        public string? Observaciones { get; set; }
    }
}

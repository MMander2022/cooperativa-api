namespace CooperativaApp.DTOS
{
    // En su DTO de entrada (Request)
    public class RegistrarPagoDTO
    {
        public int IdCredito { get; set; }
        public decimal Monto { get; set; }
        public int IdMedioPago { get; set; } // 🎯 Cambio de string a int
        public string Referencia { get; set; }
    }
}

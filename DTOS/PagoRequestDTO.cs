namespace CooperativaApp.DTOS
{
    public class PagoRequestDTO
    {
        public int IdCredito { get; set; }
        public int IdSocio { get; set; }
        public decimal Monto { get; set; }
        public int IdCaja { get; set; }
        public string Modalidad { get; set; } // "EFECTIVO" | "TRANSFERENCIA"
    }
}

namespace CooperativaApp.Models
{
    public class Operacion
    {
        public int IdOperacion { get; set; }
        public int IdCredito { get; set; }
        public string TipoOperacion { get; set; } // DESEMBOLSO, PAGO_CUOTA
        public decimal Monto { get; set; }
        public DateTime FechaOperacion { get; set; }
        public int IdUsuario { get; set; }
        public int? IdMovimientoCaja { get; set; }
        public string Observacion { get; set; }

        // Navegación
        public virtual Credito Credito { get; set; }
    }
}

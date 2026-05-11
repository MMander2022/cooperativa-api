namespace CooperativaApp.Models
{

    public class MovimientoRequest
    {
        public int IdConcepto { get; set; }
        public int? IdReferencia { get; set; }
        public decimal Monto { get; set; }
        public int IdUsuario { get; set; }
        public string Comentario { get; set; }
    }
}

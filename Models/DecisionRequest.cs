namespace CooperativaApp.Models
{
    public class DecisionRequest
    {
        public int UsuarioId { get; set; }
        public string Comentario { get; set; }
        public string Accion { get; set; } // "APROBAR", "RECHAZAR", "OBSERVAR"
    }
}

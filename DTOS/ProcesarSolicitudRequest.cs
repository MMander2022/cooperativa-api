namespace CooperativaApp.DTOS
{
    public class ProcesarSolicitudRequest
    {
        public int IdSolicitud { get; set; }
        public string Accion { get; set; }
        public string? Motivo { get; set; }
    }
}

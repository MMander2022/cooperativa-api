namespace CooperativaApp.DTOS
{
    public class SolicitudSocioHistorialDto
    {
        public int IdSolicitud { get; set; }
        public decimal MontoSolicitado { get; set; }
        public string TipoRetiro { get; set; } = "PARCIAL";
        public DateTime FechaSolicitud { get; set; }
        public string Estado { get; set; } = "PENDIENTE";
        public string? ComentarioCaja { get; set; }
    }
}

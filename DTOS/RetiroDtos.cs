namespace CooperativaApp.DTOs
{
    public class SolicitudRetiroDto
    {
        public int IdSocio { get; set; }
        public decimal MontoSolicitado { get; set; }
    }

    public class EvaluacionRetiroDto
    {
        public int IdSolicitud { get; set; }
        public string Estado { get; set; } // APROBADO o RECHAZADO
        public string? MotivoRechazo { get; set; }
        public int? IdMedioPago { get; set; } // Obligatorio si aprueba
        public int IdUsuario { get; set; }
    }

    public class RetiroItemResponse
    {
        public int IdSolicitud { get; set; }
        public int IdSocio { get; set; }
        public string SocioNombre { get; set; }
        public string PeriodoNombre { get; set; }
        public decimal Monto { get; set; }
        public string Fecha { get; set; }
        public string Estado { get; set; }
        public string Motivo { get; set; }
    }
}
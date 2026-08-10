namespace CooperativaApp.DTOs
{
    public class ItemSolicitudMasivaDto
    {
        public int IdSocio { get; set; }
        public decimal MontoSolicitado { get; set; }
    }

    public class SolicitudMasivaPayloadDto
    {
        public int IdPeriodoConfig { get; set; }
        public List<ItemSolicitudMasivaDto> Solicitudes { get; set; } = new();
    }

    public class SocioHabilitadoUtilidadDto
    {
        public int IdSocio { get; set; }
        public string Documento { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public decimal TotalUtilidadGenerada { get; set; }
        public decimal TopeMaximoRetiro { get; set; }
        public decimal SolicitadoEnCurso { get; set; }
        public decimal SaldoDisponibleRetiro { get; set; }
    }
}
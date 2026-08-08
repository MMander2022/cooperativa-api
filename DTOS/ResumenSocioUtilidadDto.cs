namespace CooperativaApp.DTOS
{
    public class ResumenSocioUtilidadDto
    {
        public decimal TotalUtilidadAcumulada { get; set; }
        public decimal PorcentajePermitido { get; set; } = 75.00m;
        public decimal TopeMaximoRetiro { get; set; }
        public decimal SaldoDisponibleRetiro { get; set; }
        public decimal MontoSolicitadoEnCurso { get; set; }
        public bool DentroDeFechaVentanilla { get; set; }
        public bool SocioHabilitado { get; set; } = true;
        public string MensajeInhabilitacion { get; set; } = string.Empty;
        public string NombrePeriodo { get; set; } = string.Empty;
        public int IdPeriodoConfig { get; set; }
        public DateTime? FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public List<SolicitudSocioHistorialDto> SolicitudesPrevias { get; set; } = new();
    }
}

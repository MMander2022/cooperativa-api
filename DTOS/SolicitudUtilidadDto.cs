namespace CooperativaApp.DTOS
{
    public class SolicitudUtilidadDto
    {
        public int IdSocio { get; set; }
        public int IdPeriodoConfig { get; set; }
        public decimal MontoSolicitado { get; set; }
        public string TipoRetiro { get; set; }
    }
}

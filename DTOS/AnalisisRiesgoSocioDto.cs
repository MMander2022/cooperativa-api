namespace CooperativaApp.DTOS
{
    public class AnalisisRiesgoSocioDto
    {
        public int IdSocio { get; set; }
        public string NombreSocio { get; set; } = string.Empty;
        public int CreditosVigentesCount { get; set; }
        public int CreditosCanceladosCount { get; set; }
        public decimal DeudaTotalVigente { get; set; }
        public string DictamenSugerido { get; set; } = "APROBAR_DIRECTO";
        public List<DetalleCreditoRiesgoDto> Creditos { get; set; } = new();
    }
}

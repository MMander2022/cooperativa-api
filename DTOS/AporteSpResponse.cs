namespace CooperativaApp.DTOS
{
    public class AporteSpResponse
    {
        public int IdAporte { get; set; }
        public int IdSocio { get; set; }
        public int MesAportado { get; set; }
        public int AnioAportado { get; set; }
        public decimal MontoPagado { get; set; }
        public DateTime FechaPago { get; set; }
        public string NombresSocio { get; set; } = string.Empty;
        public string DniSocio { get; set; } = string.Empty;
    }
}

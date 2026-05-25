namespace CooperativaApp.DTOS
{
    public class AporteRequestDto
    {
        public int IdSocio { get; set; }
        public int IdConfig { get; set; }
        public int IdMedioPago { get; set; }
        public int CantidadAcciones { get; set; }
        public decimal MontoPagado { get; set; }
        public int MesAportado { get; set; }
        public int AnioAportado { get; set; }
        public string EstadoPago { get; set; }
        public int IdUsuarioRegistro { get; set; }

        // 🎯 Este nombre debe coincidir EXACTAMENTE con el .append("ArchivoVoucher", file) del React
        public IFormFile? ArchivoVoucher { get; set; }
    }
}

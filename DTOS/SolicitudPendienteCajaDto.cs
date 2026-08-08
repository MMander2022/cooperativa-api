namespace CooperativaApp.DTOS
{
    public class SolicitudPendienteCajaDto
    {
        public int IdSolicitud { get; set; }
        public int IdSocio { get; set; }
        public string SocioNombreCompleto { get; set; } = string.Empty;
        public int IdPeriodoConfig { get; set; }
        public string NombrePeriodo { get; set; } = string.Empty;
        public decimal MontoSolicitado { get; set; }
        public string TipoRetiro { get; set; } = string.Empty;
        public DateTime FechaSolicitud { get; set; }
        public string Estado { get; set; } = string.Empty;
        public decimal TotalUtilidad { get; set; } // 🎯 Nueva columna
        public decimal MontoTope { get; set; }     // 🎯 Nueva columna (ej: 75%)
    }
}

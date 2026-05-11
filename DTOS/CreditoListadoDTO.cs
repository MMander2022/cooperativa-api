namespace CooperativaApp.DTOS
{
    public class CreditoListadoDTO
    {
        public int IdCredito { get; set; }
        public string NombreSocio { get; set; }
        public string? Dni { get; set; }
        public decimal Monto { get; set; }
        public decimal? MontoDesembolsado { get; set; }
        public decimal TasaInteres { get; set; }
        public int PlazoMeses { get; set; }
        public DateTime? FechaAprobacion { get; set; }
        public string Estado { get; set; }
        public string TipoCalculo { get; set; }
        public string ProductoNombre { get; set; } // Opcional pero recomendado
    }
}

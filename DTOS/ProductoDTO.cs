namespace CooperativaApp.DTOS
{
    public class ProductoDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string TipoAmortizacion { get; set; } // FRANCES o ALEMAN
        public decimal TasaReferencial { get; set; }
        public List<TasaRangoDTO> Rangos { get; set; } = new();
    }
}

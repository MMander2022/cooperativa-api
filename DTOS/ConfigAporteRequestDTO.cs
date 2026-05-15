namespace CooperativaApp.DTOS
{
    public class ConfigAporteRequestDTO
    {
        public decimal ValorAccion { get; set; }

        public DateTime FechaInicio { get; set; }

        public DateTime? FechaFin { get; set; }

        public bool Estado { get; set; }
    }
}
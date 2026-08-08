namespace CooperativaApp.DTOS
{
    public class RegistrarPagoSocioDto
    {
        public int IdCredito { get; set; }
        public decimal Monto { get; set; }
        public int IdMedioPago { get; set; }
        public string MedioPago { get; set; }
        public string Referencia { get; set; }
        public bool EsPrecancelacionTotal { get; set; }
        public List<int> CuotasSeleccionadas { get; set; } = new List<int>();
    }
}

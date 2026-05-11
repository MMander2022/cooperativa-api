namespace CooperativaApp.Models
{
    public class PagoRequest
    {
        public int IdCredito { get; set; }
        public int IdSocio { get; set; }
        public decimal MontoTotal { get; set; }
        public int IdUsuario { get; set; }
        public List<DetallePagoRequest> Detalles { get; set; }
    }

    public class DetallePagoRequest
    {
        public int IdCuota { get; set; }
        public decimal Monto { get; set; }
    }
    public class DetalleRequest
    {
        public int IdCuota { get; set; }

        public int? IdMora { get; set; }

        public decimal Capital { get; set; }

        public decimal Interes { get; set; }

        public decimal Mora { get; set; }
    }
}

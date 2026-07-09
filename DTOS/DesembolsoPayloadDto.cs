namespace CooperativaApp.DTOS
{
    public class DesembolsoPayloadDto
    {
        public int IdSolicitud { get; set; }
        public int IdUsuarioCaja { get; set; }
        public string Comentario { get; set; }
        public List<MedioPagoDesgloseDto> MediosPago { get; set; }
    }
}

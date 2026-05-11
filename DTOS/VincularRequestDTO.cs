namespace CooperativaApp.DTOS
{
    public class VincularRequestDTO
    {
        public int IdFamiliar { get; set; }
        public int IdParentesco { get; set; }
        public int? IdSocioTitular { get; set; }
        public int? IdMedioPago { get; set; }
    }
}

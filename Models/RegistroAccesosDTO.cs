namespace CooperativaApp.Models
{
    public class RegistroAccesosDTO
    {
        public int IdPerfil { get; set; }
        public List<int> IdsModulos { get; set; } = new();
    }
}

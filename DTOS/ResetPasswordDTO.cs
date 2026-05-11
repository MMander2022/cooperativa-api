namespace CooperativaApp.DTOS
{
    public class ResetPasswordDTO
    {
        public int IdUsuario { get; set; }
        public string NuevaPassword { get; set; } = null!;
    }
}

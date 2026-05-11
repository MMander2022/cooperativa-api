namespace CooperativaApp.DTOS
{
    public class UsuarioRegistroDto
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string NombreCompleto { get; set; } = null!;

        // 📧 Nuevo campo para capturar el correo en el registro
        public string? Email { get; set; }

        public int IdPerfil { get; set; } // 1 para Admin, 2 para Cajero, etc.
    }
}
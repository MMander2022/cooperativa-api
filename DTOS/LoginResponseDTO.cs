// DTOS/LoginResponseDto.cs
namespace CooperativaApp.DTOS
{
    public class LoginResponseDto
    {
        public UserSessionDto Usuario { get; set; } = null!;
        public List<MenuDto> Menu { get; set; } = new();
        public string Token { get; set; } = string.Empty;
    }

    public class UserSessionDto
    {
        public int IdUsuario { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Perfil { get; set; } = string.Empty;
        public int? IdSocio { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public bool RequiereCambioPassword { get; set; }
    }

    public class MenuDto
    {
        public int IdModulo { get; set; }    // 👈 ¡OBLIGATORIO!
        public string Nombre { get; set; } = string.Empty;
        public string? Ruta { get; set; }    // 👈 Puede ser null en los padres
        public string Icono { get; set; } = string.Empty;
        public int Orden { get; set; }
        public int IdPadre { get; set; }    // 👈 ¡OBLIGATORIO PARA AGRUPAR!
    }
}
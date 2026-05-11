using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; }

        public string Username { get; set; } = null!;

        // 🛡️ Seguridad: Ambos son arreglos de bytes (byte[])
        public byte[] PasswordHash { get; set; } = null!;
        public byte[] PasswordSalt { get; set; } = null!;

        // 📧 Comunicación: El nuevo campo galáctico
        public string? Email { get; set; }

        public string NombreCompleto { get; set; } = null!;

        public int IdPerfil { get; set; }

        public bool Estado { get; set; }
        public int IntentosFallidos { get; set; } = 0;
        public bool IsLocked { get; set; } = false;
        public DateTime? UltimoLogin { get; set; }
        public DateTime? UltimoAcceso { get; set; }

        public int? IdSocio { get; set; }
        public bool RequiereCambioPassword { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        // 🔗 Navegación (Relación con la tabla Perfil)
        [ForeignKey("IdPerfil")] // 🚀 ESTO ELIMINA EL ERROR 'PerfilIdPerfil'
        public Perfil Perfil { get; set; } = null!;
        [ForeignKey("IdSocio")]
        public virtual Socio? Socio { get; set; } // 👈 Esta propiedad DEBE existir para que el .Include funcione
    }
}

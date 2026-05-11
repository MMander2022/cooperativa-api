using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    public class Modulo
    {
        [Key]
        public int IdModulo { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Ruta { get; set; } = string.Empty;
        public string Icono { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;
    }

    public class PerfilModulo
    {
        public int IdPerfil { get; set; }
        public int IdModulo { get; set; }

        [ForeignKey("IdModulo")]
        public virtual Modulo Modulo { get; set; }
    }
}

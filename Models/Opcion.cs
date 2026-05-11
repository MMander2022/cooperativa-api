using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    [Table("Opciones")]
    public class Opcion
    {
        [Key] // 🛡️ El escudo contra el error "requires a primary key"
        public int IdOpcion { get; set; }

        [Required]
        public string Nombre { get; set; } = null!;

        public string? Url { get; set; }
        public string? Icono { get; set; }
        public string? Modulo { get; set; }
        public bool Estado { get; set; } = true;
    }
}

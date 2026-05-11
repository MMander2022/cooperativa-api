using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    [Table("Parentesco")] // 🎯 Asegura que apunte exactamente al nombre en la BD
    public class Parentesco
    {
        [Key]
        public int IdParentesco { get; set; }

        [Required]
        [StringLength(50)]
        public string Descripcion { get; set; }

        // Propiedad de navegación (opcional, por si quieres ver todos los vínculos desde Parentesco)
        public virtual ICollection<Familiaridad> Familiaridades { get; set; }
    }
}

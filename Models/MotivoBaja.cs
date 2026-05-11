using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    [Table("MotivosBaja")] // Asegúrate que el nombre coincida con tu DB
    public class MotivoBaja
    {
        [Key] // 👈 ESTO ES LO QUE FALTA
        public int IdMotivo { get; set; }

        [Required]
        public string Descripcion { get; set; }

        public bool Activo { get; set; } = true;

        public bool RequiereComentario { get; set; } = false;
    }
}
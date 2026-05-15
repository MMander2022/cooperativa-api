using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    [Table("ConfigAportes")]
    public class ConfigAporte
    {
        [Key]
        public int IdConfig { get; set; }
        public decimal ValorAccion { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public bool Estado { get; set; }

        // Auditoría
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; } = DateTime.Now;
        public int? IdUsuarioRegistro { get; set; }
        public int? IdUsuarioModificacion { get; set; }
    }
}

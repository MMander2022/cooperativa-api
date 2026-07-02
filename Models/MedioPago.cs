using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    [Table("MediosPago")]
    public class MedioPago
    {
        [Key]
        public int IdMedioPago { get; set; }
        public string? Nombre { get; set; } = null!;
    }
}

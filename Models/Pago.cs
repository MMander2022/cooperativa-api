using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
namespace CooperativaApp.Models
{
    [Table("Pagos")]
    public class Pago
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdPago { get; set; }
        [Required]
        public int IdCredito { get; set; } // El vínculo que faltaba

        [Required]
        public int IdSocio { get; set; }

        public decimal MontoTotal { get; set; }
        public DateTime FechaPago { get; set; } = DateTime.Now;
        public int IdUsuario { get; set; }
        // 💎 Nuevo campo vinculado a la BD
        public int? IdMedioPago { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string NroOperacion { get; set; }

        // Propiedades de navegación (Para consultas ricas en datos)
        [ForeignKey("IdCredito")]
        public virtual Credito Credito { get; set; }

        [ForeignKey("IdMedioPago")]
        public virtual MedioPago MedioPago { get; set; }
    }
}

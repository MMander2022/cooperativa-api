using CooperativaApp.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    [Table("PagosDetalle")]
    public class DetallePago
    {
        [Key]
        [Column("IdPagoDetalle")]
        public int IdDetallePago { get; set; }

        [Required]
        [Column("IdPago")]
        public int IdPago { get; set; }

        [Required]
        [Column("IdCuota")]
        public int IdCuota { get; set; }

        // Campos necesarios según la estructura de BD para trazabilidad total
        /*[Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoPagado { get; set; } // Suma de capital + interés + otros*/

        [Required]
        [Column("IdConcepto")] // 🔗 Nuevo: Mapeo del concepto
        public int IdConcepto { get; set; }

        [Required]
        [Column("Monto", TypeName = "decimal(18,2)")] // 🔗 Vincula MontoPagado con Monto en SQL
        public decimal Monto { get; set; }
        /*
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CapitalPagado { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal InteresPagado { get; set; }*/

        [NotMapped]
        public decimal CapitalPagado
        {
            get { return IdConcepto == 1 ? Monto : 0; }
        }

        [NotMapped]
        public decimal InteresPagado
        {
            get { return IdConcepto == 2 ? Monto : 0; }
        }
        // Relaciones de Navegación (Engranaje)
        [ForeignKey("IdPago")]
        public virtual Pago Pago { get; set; }

        [ForeignKey("IdCuota")]
        public virtual Cuota Cuota { get; set; }
    }
}
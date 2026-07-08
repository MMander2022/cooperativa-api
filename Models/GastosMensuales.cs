using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    [Table("GastosMensuales")]
    public class GastosMensuales
    {
        [Key]
        public int IdGasto { get; set; }

        [Required]
        public int Anio { get; set; }

        [Required]
        public int Mes { get; set; }

        [Required]
        public int IdTipoGasto { get; set; }

        [ForeignKey("IdTipoGasto")]
        public TipoGasto? TipoGasto { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [Required]
        public int IdUsuarioRegistro { get; set; }
    }
}
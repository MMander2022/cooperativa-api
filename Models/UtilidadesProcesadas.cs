using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    [Table("UtilidadesProcesadas")]
    public class UtilidadesProcesadas
    {
        [Key]
        public int IdUtilidad { get; set; }

        [Required]
        public int IdPeriodoConfig { get; set; }

        [ForeignKey("IdPeriodoConfig")]
        public PeriodosRetiroUtilidad? PeriodoConfig { get; set; }

        [Required]
        public int IdSocio { get; set; }

        [Required]
        public int Anio { get; set; }

        [Required]
        public int Mes { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AporteAcumuladoCorte { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,6)")]
        public decimal FactorProrrateo { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoUtilidadGenerada { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoDisponible { get; set; }

        public DateTime FechaProcesado { get; set; } = DateTime.Now;
    }
}
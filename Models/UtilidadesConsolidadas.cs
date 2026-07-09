using CooperativaApp.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaDB.Models
{
    [Table("UtilidadesConsolidadas")]
    public class UtilidadesConsolidadas
    {
        [Key]
        public int IdConsolidado { get; set; }

        public int IdPeriodoConfig { get; set; }

        public int IdSocio { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalUtilidadAcumulada { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal SaldoUtilidad { get; set; }

        public DateTime FechaProceso { get; set; }

        public int IdUsuarioProcesado { get; set; }

        // Propiedades de navegación
        [ForeignKey("IdPeriodoConfig")]
        public virtual PeriodosRetiroUtilidad PeriodoConfig { get; set; }
    }
}
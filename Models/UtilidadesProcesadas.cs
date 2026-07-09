using CooperativaApp.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaDB.Models
{
    [Table("UtilidadesProcesadas")]
    public class UtilidadesProcesadas
    {
        [Key]
        public int IdUtilidad { get; set; }

        public int IdPeriodoConfig { get; set; }

        public int IdSocio { get; set; }

        public int Anio { get; set; }

        public int Mes { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? AporteAcumuladoCorte { get; set; }

        [Column(TypeName = "decimal(18, 6)")]
        public decimal? FactorProrrateo { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? MontoUtilidadGenerada { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? MontoDisponible { get; set; }

        // 💎 Tus nuevos campos oficiales del detalle alineados
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? AporteAcumuladoMes { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? UtilidadObtenida { get; set; } // Respetando el typo de tu BD

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? InteresMensualRepartir { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? AporteAcumuladoFinal { get; set; } // 💎 Inyectado para solucionar el error de definición

        // Propiedades de navegación (Opcional, por si haces un .Include en LINQ)
        [ForeignKey("IdPeriodoConfig")]
        public virtual PeriodosRetiroUtilidad PeriodoConfig { get; set; }
    }
}
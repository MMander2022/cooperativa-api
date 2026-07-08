using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    [Table("SolicitudUtilidad")]
    public class SolicitudUtilidad
    {
        [Key]
        public int IdSolicitud { get; set; }

        [Required]
        public int IdSocio { get; set; }

        [Required]
        public int IdPeriodoConfig { get; set; }

        [ForeignKey("IdPeriodoConfig")]
        public PeriodosRetiroUtilidad? PeriodoConfig { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoSolicitado { get; set; }

        [Required]
        [StringLength(20)]
        public string TipoRetiro { get; set; } = "PARCIAL"; // TOTAL o PARCIAL

        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        [StringLength(20)]
        public string Estado { get; set; } = "PENDIENTE"; // PENDIENTE, APROBADO, RECHAZADO

        [StringLength(250)]
        public string? ComentarioCaja { get; set; }

        public DateTime? FechaProcesadoCaja { get; set; }

        public int? IdUsuarioCaja { get; set; }
    }
}
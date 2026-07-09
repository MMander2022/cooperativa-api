// Models/SolicitudUtilidad.cs
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
        public int IdSocio { get; set; }
        public int IdPeriodoConfig { get; set; }
        public decimal MontoSolicitado { get; set; }
        public string TipoRetiro { get; set; }
        public DateTime? FechaSolicitud { get; set; }
        public string Estado { get; set; }
        public string ComentarioCaja { get; set; }
        public DateTime? FechaProcesadoCaja { get; set; }
        public int? IdUsuarioCaja { get; set; }

        // Propiedad de navegación por si necesitas hacer .Include(s => s.PeriodoConfig)
        [ForeignKey("IdPeriodoConfig")]
        public virtual PeriodosRetiroUtilidad PeriodoConfig { get; set; }
    }
}
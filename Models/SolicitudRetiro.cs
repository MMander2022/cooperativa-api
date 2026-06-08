using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    public class SolicitudRetiro
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("IdSolicitud")]
        public int IdSolicitud { get; set; }
        public int IdSocio { get; set; }
        public int IdPeriodo { get; set; }
        public decimal MontoSolicitado { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public string Estado { get; set; } // PENDIENTE, APROBADO, RECHAZADO
        public string? MotivoRechazo { get; set; }
        public int? IdUsuarioAuditoria { get; set; }
        public DateTime? FechaAuditoria { get; set; }

        public virtual Socio Socio { get; set; }
        public virtual PeriodoRetiro PeriodoRetiro { get; set; }
    }
}

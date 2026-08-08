using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    [Table("SolicitudPagoDetalle")]
    public class SolicitudPagoDetalle
    {
        [Key] // 🚀 ESTO ELIMINA EL ERROR DE INICIO DE SESIÓN
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdSolicitudDetalle { get; set; }
        public int IdSolicitud { get; set; }
        public int IdCuota { get; set; }
        public decimal MontoAplicado { get; set; }
        // 💎 Nueva propiedad para la Lupa Diamante
        public DateTime? FechaSolicitud { get; set; }
        public decimal InteresCubierto { get; set; }
        public decimal? MoraCubierta { get; set; }
        
        public decimal CapitalCubierto { get; set; }

        [ForeignKey("IdSolicitud")]
        public virtual SolicitudPagoSocio? Solicitud { get; set; }
        [ForeignKey("IdCuota")]
        public virtual Cuota? Cuota { get; set; }
    }
}

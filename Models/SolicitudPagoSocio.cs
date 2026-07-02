using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    public class SolicitudPagoSocio
    {
        [Key]
        public int IdSolicitud { get; set; }
        public int IdCredito { get; set; }
        public int IdSocio { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaSolicitud { get; set; } = DateTime.Now;
        public string MedioPago { get; set; } // EFECTIVO, TRANSFERENCIA, YAPE, PLIN
        public int IdMedioPago { get; set; }
        public string? ReferenciaOperacion { get; set; }
        public string? ComprobanteUrl { get; set; } = string.Empty;
        //public IFormFile? ComprobanteUrl { get; set; }
        public int IdEstado { get; set; } // 1: Pendiente, 2: Procesado, 3: Rechazado
        public string? ObservacionesCajero { get; set; }
        public DateTime? FechaProcesamiento { get; set; }
        public int? IdUsuarioCajero { get; set; }

        // Navegación Diamante
        [ForeignKey("IdCredito")]
        public virtual Credito? Credito { get; set; }
        [ForeignKey("IdSocio")]
        public virtual Socio? Socio { get; set; }
        [ForeignKey("IdMedioPago")]
        public virtual MedioPago? MediosPago { get; set; }
    }
}

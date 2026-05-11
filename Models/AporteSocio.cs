
using Microsoft.EntityFrameworkCore; 
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CooperativaApp.Models
{
    [Table("AportesSocios")]
    public class AporteSocio
    {
        [Key]
        public int IdAporte { get; set; }
        public int IdSocio { get; set; }
        public int IdConfig { get; set; }
        public int MesAportado { get; set; }
        public int AnioAportado { get; set; }
        public int CantidadAcciones { get; set; } // 🚀 Dato que ingresa el Socio
        public decimal MontoPagado { get; set; }
        public DateTime FechaPago { get; set; } = DateTime.Now;
        public int IdUsuarioRegistro { get; set; }  
        public char EstadoPago { get; set; } = 'P';
        public string? UrlEvidencia { get; set; } = string.Empty;
        public string? ComentarioCaja { get; set; }
        public DateTime? FechaValidacion { get; set; }
        public int? IdUsuarioValidador { get; set; }
        public int? IdMovimientoCaja { get; set; }
        public int? IdMedioPago { get; set; } // Nuevo campo

        [ForeignKey("IdMedioPago")]
        public virtual MedioPago? MedioPago { get; set; } // Propiedad de navegación
        // Navegación
        [JsonIgnore]
        public virtual Socio? Socio { get; set; } = null!;
        [JsonIgnore]
        [ForeignKey("IdConfig")]
        public virtual ConfigAporte? ConfigAporte { get; set; }
    }
}

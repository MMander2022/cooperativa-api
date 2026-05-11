using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CooperativaApp.Models
{
    [Table("HistorialEstadoSocio")]
    public class HistorialEstadoSocio
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        // 🔥 QUITAMOS EL JSONIGNORE DE AQUÍ
        public int IdHistorial { get; set; }

        public int IdSocio { get; set; }
        public int IdUsuarioAccion { get; set; }
        public bool? EstadoAnterior { get; set; }
        public bool? EstadoNuevo { get; set; }
        public int? IdMotivo { get; set; }
        public string? Comentario { get; set; }
        public DateTime FechaAccion { get; set; } = DateTime.Now;
        [ForeignKey("IdSocio")]
        public virtual Socio Socio { get; set; }
        // Propiedades de Navegación
        // Aquí podrías usar [JsonIgnore] si al cargar el historial NO quieres 
        // traer los detalles del motivo automáticamente para ahorrar ancho de banda.
        public virtual MotivoBaja? Motivo { get; set; }
    }
}
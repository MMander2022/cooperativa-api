using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CooperativaApp.Models
{
    [Table("Socio")]
    public class Socio
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("IdSocio")]
        // Retiramos [JsonIgnore] de aquí. El ID es vital para el Front-end.
        public int IdSocio { get; set; }

        [Required]
        [StringLength(8)]
        public string DNI { get; set; }

        [Required]
        public string Nombres { get; set; }

        // Mantenemos Apellidos por compatibilidad, pero priorizamos los nuevos campos
        //public string? Apellidos { get; set; }
        public string Apellidos { get; set; } = null!;

        public string? ApellidoPaterno { get; set; }
        public string? ApellidoMaterno { get; set; }

        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string correo { get; set; }

        public DateTime? FechaNacimiento { get; set; }
        public DateTime? FechaRegistro { get; set; } = DateTime.Now;
        public bool Estado { get; set; } = true;

        // Campos de Auditoría
        public int? IdUsuarioRegistro { get; set; }
        public int? IdUsuarioModificacion { get; set; }
        public DateTime? FechaModificacion { get; set; } = DateTime.Now;
        public bool? PermiteRetiro { get; set; } = false;

        // 🔗 Relación con Historial (Opcional, pero recomendada)
        [JsonIgnore] // Aquí sí va el Ignore para evitar ciclos infinitos
        public virtual ICollection<HistorialEstadoSocio>? HistorialEstados { get; set; }

    }
}
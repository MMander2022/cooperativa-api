using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    [Table("Solicitudes", Schema = "dbo")]
    public class SolicitudCredito
    {
        [Key]
        public int Id { get; set; } // El nombre coincide, no necesita [Column]

        public int SocioId { get; set; }

        public int ProductoId { get; set; }

        public decimal MontoSolicitado { get; set; }

        // OJO: En tu SQL es 'PlazoSolicitado', en tu C# era 'PlazoMeses'
        [Column("PlazoSolicitado")]
        public int PlazoMeses { get; set; }

        // OJO: En tu SQL es 'TasaPropuesta', en tu C# era 'TasaAplicada'
        [Column("TasaPropuesta")]
        public decimal TasaAplicada { get; set; }

        public string? Estado { get; set; }

        // OJO: En tu SQL es 'FechaRegistro', en tu C# era 'FechaCreacion'
        [Column("FechaRegistro")]
        public DateTime? FechaCreacion { get; set; }

        public int? UsuarioCreadorId { get; set; }

        public string? Observaciones { get; set; }
        [ForeignKey("ProductoId")]
        public virtual Producto Producto { get; set; }
        public int? UsuarioEvaluador { get; set; }
        public DateTime? FechaEvaluacion { get; set; }
        public string? ComentarioEvaluador { get; set; }
        // 🛰️ EL PUENTE (Propiedad de Navegación)
        [ForeignKey("SocioId")]
        public virtual Socio Socio { get; set; }
    }
}
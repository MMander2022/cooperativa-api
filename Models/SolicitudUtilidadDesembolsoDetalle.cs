// Models/SolicitudUtilidadDesembolsoDetalle.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    [Table("SolicitudUtilidadDesembolsoDetalle")]
    public class SolicitudUtilidadDesembolsoDetalle
    {
        [Key]
        public int IdDesembolsoDetalle { get; set; }
        public int IdSolicitud { get; set; }
        public int IdMedioPago { get; set; }
        public decimal MontoDesembolsado { get; set; }
        public string? ReferenciaOperacion { get; set; }
        public DateTime? FechaDesembolso { get; set; }
    }
}
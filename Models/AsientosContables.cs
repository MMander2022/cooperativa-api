using System.ComponentModel.DataAnnotations;

namespace CooperativaApp.Models
{
    public class AsientosContables
    {
        [Key]
        public int IdAsiento { get; set; }
        public DateTime Fecha { get; set; }
        public string Glosa { get; set; } // Descripción del asiento
        public int? IdReferencia { get; set; }
        public string Origen { get; set; } // "CAJA", "PLANILLA", etc.
        public List<DetalleAsiento> Detalles { get; set; }
    }
}

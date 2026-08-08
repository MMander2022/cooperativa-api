using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    public class MovimientoCaja
    {
        [Key]
        public int IdMovimiento { get; set; }
        public int IdConcepto { get; set; }

        // Aquí estaban los errores: faltaba IdAsiento e IdReferencia (que es el IdCredito)
        public int? IdCredito { get; set; }
       public int? IdAsiento { get; set; }
        public DateTime? FechaMovimiento { get; set; }

        public String? Estado { get; set; }
        public decimal Monto { get; set; }
        public DateTime? Fecha { get; set; } = DateTime.Now;
        public int IdUsuario { get; set; }
        public int? IdCaja { get; set; }
        public int? IdMedioPago { get; set; }

        [ForeignKey("IdConcepto")]
        public virtual ConceptoOperacion? Concepto { get; set; }
    }

}

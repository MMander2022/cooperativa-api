using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YourProject.Models;

namespace CooperativaApp.Models
{
    public class ConceptoOperacion
    {
        [Key]
        public int IdConcepto { get; set; }
        [Required]
        public string Nombre { get; set; } // Ej: "Desembolso de Crédito"
        [Required]
        public string TipoMovimiento { get; set; } // "I" para Ingreso, "E" para Egreso

        //[ForeignKey("CuentaContableDebe")]
        public string? CuentaContableDebe { get; set; }
       
         public virtual CuentaContable CuentaContableDebeNavigation { get; set; }
       // [ForeignKey("CuentaContableHaber")]
        public string? CuentaContableHaber { get; set; }
        
        public virtual CuentaContable CuentaContableHaberNavigation { get; set; }
        public bool? GeneraAsiento { get; set; } // 👈 Anulable porque en BD hay NULLs

        public string? Estado { get; set; } // 👈 Anulable porque en BD hay NULLs
    }
}

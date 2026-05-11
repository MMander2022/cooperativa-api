using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    public class DetalleAsiento
    {
        [Key]
        public int IdDetalle { get; set; }
        public int IdAsiento { get; set; }

        // El error decía que no existía CodigoCuenta
        public string CuentaContable { get; set; }
        public decimal Debe { get; set; }
        public decimal Haber { get; set; }

        [ForeignKey("IdAsiento")]
        public virtual AsientosContables Asiento { get; set; }
    }
}

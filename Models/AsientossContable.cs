using System.ComponentModel.DataAnnotations;

namespace CooperativaApp.Models
{
    public class AsientossContable
    {
        [Key]
        public int IdAsiento { get; set; }

        public DateTime Fecha { get; set; }

        public string TipoOperacion { get; set; } = string.Empty;

        public int ReferenciaId { get; set; }

        public string CuentaDebe { get; set; } = string.Empty;

        public string CuentaHaber { get; set; } = string.Empty;

        public decimal Monto { get; set; }

        public string Estado { get; set; } = "GENERADO";
    }
}

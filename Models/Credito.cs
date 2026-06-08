using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    [Table("Credito")]
    public class Credito
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdCredito { get; set; }

        public int IdSocio { get; set; }

        public decimal Monto { get; set; }
        public decimal? MontoDesembolsado { get; set; }

        public decimal TasaInteres { get; set; }
        public decimal SaldoCapital { get; set; }

        public int PlazoMeses { get; set; }

        public DateTime FechaSolicitud { get; set; }

        public DateTime? FechaAprobacion { get; set; }
        public DateTime? FechaDesembolso { get; set; }
        public DateTime? FechaUltimoDesembolso { get; set; }
        public List<Cuota>? Cuotas { get; set; }
        public string Estado { get; set; }
        public string? EstadoCredito { get; set; }
        public string TipoCalculo { get; set; } = "FRANCES";
        
        [ForeignKey("IdSocio")]
        public virtual Socio Socio { get; set; }

    }

}

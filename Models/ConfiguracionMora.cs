using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    public class ConfiguracionMora
    {
        [Key]
        [Column("IdConfiguracion")]
        public int Id { get; set; }
        [Column("TipoMora")]
        public string Tipo { get; set; } = "DIARIA";

        public decimal Tasa { get; set; }
        public decimal? MontoFijo { get; set; }

        public int DiasGracia { get; set; }

        public bool Activo { get; set; }

        public string TipoAplicacion { get; set; } = "INDEPENDIENTE";
    }
}

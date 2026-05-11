using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
namespace CooperativaApp.Models
{
    public class Mora
    {

        [Key]
        public int IdMora { get; set; }

        public int IdCuota { get; set; }

        public DateTime FechaGeneracion { get; set; }

        public DateTime FechaInicio { get; set; }

        public DateTime? FechaFin { get; set; }

        public int? DiasMora { get; set; }

        public decimal MontoMora { get; set; }

        public decimal? MontoPagado { get; set; }

        public string? Estado { get; set; }

        public decimal? SaldoMora { get; set; }

        // navegación
        public Cuota? Cuota { get; set; }

        public List<DetallePago>? Detalles { get; set; }
    }
}

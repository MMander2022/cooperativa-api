using System.ComponentModel.DataAnnotations;

namespace CooperativaApp.Models
{
    public class ArqueoCaja
    {
        [Key]
        public int IdArqueo { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal SaldoInicial { get; set; }
        public decimal SaldoFinalTeorico { get; set; } // Calculado
        public decimal SaldoFinalReal { get; set; }    // Ingresado por usuario
        public string Estado { get; set; } = "ABIERTO"; // ABIERTO, CERRADO
    }
}

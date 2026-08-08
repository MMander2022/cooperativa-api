using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    [Table("Cuota")]
    public class Cuota
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdCuota { get; set; }

        public int IdCredito { get; set; }

        public int NumeroCuota { get; set; }
        public bool EsPrecancelacion { get; set; }

        public DateTime FechaVencimiento { get; set; }

        public decimal Capital { get; set; }

        public decimal Interes { get; set; }
        public decimal SaldoCapital { get; set; }
        
            public decimal SaldoMora { get; set; }
        public decimal SaldoInteres { get; set; }

        public decimal MontoCuota { get; set; }
        public decimal MoraGenerada { get; set; }
        public decimal Saldo { get; set; }

        public string? Estado { get; set; }
        public List<Mora>? Moras { get; set; }

        // relación
        [ForeignKey("IdCredito")]
        public Credito? Credito { get; set; }



        // --- 💎 Propiedades Diamante (Viven solo en C# / RAM) ---

        // --- 💎 PROPIEDADES DINÁMICAS (Solo Lectura para Auditoría) ---

        // --- 💎 Propiedades Diamante (Viven solo en C# / RAM) ---

        // --- 💎 PROPIEDADES DINÁMICAS (Solo Lectura para Auditoría) ---
        [NotMapped]
        public decimal InteresPagadoReal
        {
            get
            {
                // Concepto 2 = Interés (Ajustar ID según su tabla Conceptos)
                return PagosDetalle?.Where(p => p.IdConcepto == 2).Sum(p => p.Monto) ?? 0;
            }
        }

        [NotMapped]
        public decimal CapitalPagadoReal
        {
            get
            {
                // Concepto 1 = Capital (Ajustar ID según su tabla Conceptos)
                return PagosDetalle?.Where(p => p.IdConcepto == 1).Sum(p => p.Monto) ?? 0;
            }
        }

        [NotMapped]
        public decimal TotalPagadoReal => InteresPagadoReal + CapitalPagadoReal;

        // Relación de navegación
        public virtual ICollection<DetallePago> PagosDetalle { get; set; } = new List<DetallePago>();
    }
}

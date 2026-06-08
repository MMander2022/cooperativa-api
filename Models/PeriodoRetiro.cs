using System.ComponentModel.DataAnnotations;

namespace CooperativaApp.Models
{
    public class PeriodoRetiro
    {
        [Key]
        public int IdPeriodo { get; set; }
        public string NombrePeriodo { get; set; }
        public int MesPermitido { get; set; } // 👈 Ajuste: Mes Contable (1-12)
        public int AnioFiscal { get; set; }   // 👈 Ajuste: Año Fiscal (Ej: 2026)
        public bool Activo { get; set; }
    }
}

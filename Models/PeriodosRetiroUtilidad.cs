using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    [Table("PeriodosRetiroUtilidad")]
    public class PeriodosRetiroUtilidad
    {
        [Key]
        public int IdPeriodoConfig { get; set; }

        [Required]
        [StringLength(100)]
        public string NombrePeriodo { get; set; } = string.Empty;

        [Required]
        public DateTime FechaInicioCalculo { get; set; }

        [Required]
        public DateTime FechaFinCalculo { get; set; }

        [Required]
        public DateTime FechaAperturaRetiro { get; set; }

        [Required]
        public DateTime FechaCierreRetiro { get; set; }
        // 🎯 AGREGAR ESTA LÍNEA EN TU ENTIDAD C#:
        public decimal PorcentajeMaximoRetiro { get; set; } = 75.00m;

        [StringLength(20)]
        public string Estado { get; set; } = "INACTIVO"; // CONFIGURADO, PROCESADO, HABILITADO, CERRADO
    }
}
using System.ComponentModel.DataAnnotations;

namespace CooperativaApp.DTOs
{
    public class ProcesarUtilidadDTO
    {
        [Required(ErrorMessage = "El identificador del periodo de configuración es mandatorio.")]
        public int IdPeriodoConfig { get; set; }

        [Required(ErrorMessage = "El año de evaluación es obligatorio.")]
        [Range(2020, 2050, ErrorMessage = "Año fuera del rango operativo.")]
        public int Anio { get; set; }

        [Required(ErrorMessage = "El mes de evaluación es mandatorio.")]
        [Range(1, 12, ErrorMessage = "El mes debe estar comprendido entre 1 y 12.")]
        public int Mes { get; set; }
    }
}
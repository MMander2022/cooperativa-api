using System.ComponentModel.DataAnnotations;

namespace CooperativaApp.DTOs
{
    public class SolicitudRetiroDTO
    {
        [Required(ErrorMessage = "El identificador del periodo es obligatorio.")]
        public int IdPeriodoConfig { get; set; }

        [Required(ErrorMessage = "El monto a retirar es mandatorio.")]
        [Range(0.01, 999999.99, ErrorMessage = "El monto debe ser mayor a cero.")]
        public decimal MontoSolicitado { get; set; }
    }
}
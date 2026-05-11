using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace CooperativaApp.Models

{
    [Table("Logs_Actividad")]
    public class LogActividad
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long IdLog { get; set; }
        public int? IdUsuario { get; set; }
        public string Accion { get; set; } = null!;
        public string Detalle { get; set; } = null!;
        public string IP { get; set; } = null!;
        public DateTime FechaRegistro { get; set; }
    }
}

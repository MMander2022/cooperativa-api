using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    public class Familiaridad
    {
        [Key]
        public int IdFamiliaridad { get; set; }
        public int IdSocioTitular { get; set; }
        public int IdSocioFamiliar { get; set; }
        public int IdParentesco { get; set; }
        public DateTime FechaVinculacion { get; set; }
        public bool Activo { get; set; }

        [ForeignKey("IdSocioTitular")]
        public virtual Socio SocioTitular { get; set; }
        [ForeignKey("IdSocioFamiliar")]
        public virtual Socio SocioFamiliar { get; set; }
        [ForeignKey("IdParentesco")]
        public virtual Parentesco Parentesco { get; set; }
    }
}

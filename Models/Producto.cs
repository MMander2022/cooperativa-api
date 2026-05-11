using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace CooperativaApp.Models
{
    [Table("PRODUCTO")]
    public class Producto
    {
        [Key]
        [Column("PRO_Id")]
        public int Id { get; set; }

        [Column("PRO_Nombre")]
        [StringLength(50)]
        public string? Nombre { get; set; }

        [Column("PRO_CALCULOCUOTA")]
        [StringLength(50)]
        public string? CalculoCuota { get; set; } // Ej: FRANCES, ALEMAN

        [Column("PRO_TasaReferencial")]
        public decimal? TasaReferencial { get; set; }

        [Column("PRO_Estado")]
        public bool? Estado { get; set; }

        [Column("PRO_Usuario")]
        public int? UsuarioId { get; set; }

        [Column("PRO_Descripcion")]
        [StringLength(255)]
        public string? Descripcion { get; set; }

        // Relación con las tasas (Navegación)
        public virtual ICollection<ProductoTasa> Tasas { get; set; } = new List<ProductoTasa>();

    }
}

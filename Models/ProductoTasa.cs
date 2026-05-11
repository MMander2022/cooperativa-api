using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace CooperativaApp.Models
{
    [Table("ProductoTasas")]
    public class ProductoTasa
    {
        [Key]
        public int IdTasa { get; set; }

        [Required]
        [Column("PRO_Id")]
        public int ProductoId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal MontoMinimo { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal MontoMaximo { get; set; }

        [Required]
        [Column(TypeName = "decimal(5, 2)")]
        public decimal TasaInteres { get; set; }

        [Column("PROTA_Usuario")]
        public int? UsuarioId { get; set; }

        // Propiedad de navegación hacia el Padre
        [ForeignKey("ProductoId")]
        public virtual Producto? Producto { get; set; }
    }
}

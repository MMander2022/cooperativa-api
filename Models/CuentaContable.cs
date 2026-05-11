using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YourProject.Models
{
    [Table("CuentasContables")]
    public class CuentaContable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // El código lo asignamos nosotros (ej: "1011")
        [StringLength(20)]
        public string CodigoCuenta { get; set; }

        [Required]
        [StringLength(150)]
        public string NombreCuenta { get; set; }

        [Required]
        public int Nivel { get; set; } // 1: Clase, 2: Cuenta, 3: Subcuenta, etc.

        [Required]
        [StringLength(1)]
        public string Naturaleza { get; set; } // "D" para Deudora (Activos/Gastos), "A" para Acreedora (Pasivos/Ingresos)

        public bool EsAnalitica { get; set; } // true: Permite movimientos, false: Es solo título/acumuladora

        public bool Activa { get; set; } = true;

        // Relación
        // al: Si quieres manejar jerarquía (Padre/Hijo)
        public string? CodigoCuentaPadre { get; set; }
    }
}
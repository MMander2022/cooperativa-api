
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CooperativaApp.Models
{
    [Table("Perfiles")]
    public class Perfil
    {
        [Key]
        public int IdPerfil { get; set; }
        public string Nombre { get; set; } = null!; // ADMIN, CAJERO, ANALISTA
       // public string? Descripcion { get; set; }
        public List<Opcion> Opciones { get; set; } = new();
    }
}

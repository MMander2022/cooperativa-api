using System.ComponentModel.DataAnnotations;

namespace CooperativaApp.Models
{
    public class GlobalSettings
    {
        [Key]
        public int SettingId { get; set; }
        public string SettingKey { get; set; }
        public string SettingValue { get; set; }
        public string DataType { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public bool IsSystem { get; set; }
        public DateTime LastUpdated { get; set; }
        public int? UpdatedBy { get; set; } // Opcional: null si es carga inicial
    }
}

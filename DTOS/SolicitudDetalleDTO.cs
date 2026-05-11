namespace CooperativaApp.DTOS
{
    public class SolicitudDetalleDTO
    {
        // 🆔 Identificadores Críticos
        public int Id { get; set; }
        public int SocioId { get; set; }

        // 👥 Datos de Identidad (Resueltos vía Join/Include)
        public string SocioNombre { get; set; } = string.Empty;
        public string IdentidadSocio { get; set; } = string.Empty; // Ej: DNI o Tipo de Socio

        // 💳 Detalles del Producto Financiero
        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;

        // 💰 Parámetros Económicos
        public decimal Monto { get; set; }
        public int Plazo { get; set; }
        public decimal TasaReferencial { get; set; }

        // 🚦 Estados y Auditoría
        public string Estado { get; set; } = "SOLICITADO";
        public DateTime FechaCreacion { get; set; }

        // 📝 Feedback del Analista (Vital para solicitudes OBSERVADAS)
        public string? ComentarioAnalista { get; set; }
        public DateTime? FechaRevision { get; set; }

        // 🛰️ Campos Extra para el Front (Opcionales)
        public string? TipoAmortizacion { get; set; }
    }
}

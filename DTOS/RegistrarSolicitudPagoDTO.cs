namespace CooperativaApp.DTOS
{
    public class RegistrarSolicitudPagoDTO
    {
        public int IdCredito { get; set; }
        public decimal Monto { get; set; }
        public string MedioPago { get; set; } // 'YAPE', 'PLIN', 'TRANSFERENCIA', 'DEPOSITO'
        public int IdMedioPago { get; set; } // 'YAPE', 'PLIN', 'TRANSFERENCIA', 'DEPOSITO'
        public string Referencia { get; set; } // Número de operación
        public string? Observaciones { get; set; }
        // 🎯 PROPIEDAD REQUERIDA PARA MÚLTIPLES ARCHIVOS
        public List<IFormFile>? ArchivosVouchers { get; set; }
        public IFormFile? ArchivoVoucher { get; set; }
        // 🎯 Control de Precancelación
        public bool EsPrecancelacionTotal { get; set; }
        public List<int> CuotasSeleccionadas { get; set; } = new List<int>();
    }
}

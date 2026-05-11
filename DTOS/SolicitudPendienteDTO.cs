namespace CooperativaApp.DTOs
{
    public record SolicitudPendienteDTO(
     int Id,
     string SocioNombre,
     string ProductoNombre,
     decimal Monto,
     int Plazo,
     decimal Tasa,
     decimal CuotaEstimada, // 👈 Nuevo campo
     string Estado,
     DateTime Fecha,
     string TipoAmortizacion // 🚀 NUEVO: Campo vital para el radar
 );
}
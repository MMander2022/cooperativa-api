namespace CooperativaApp.DTOs
{
    public record SolicitudPendienteDTO(
        int Id,
        int SocioId, // 🎯 PROPIEDAD CLAVE AGREGADA
        string SocioNombre,
        string ProductoNombre,
        decimal Monto,
        int Plazo,
        decimal Tasa,
        decimal CuotaEstimada,
        string Estado,
        DateTime Fecha,
        string TipoAmortizacion
    );
}
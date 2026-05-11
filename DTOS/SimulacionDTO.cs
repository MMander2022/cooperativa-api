
namespace CooperativaApp.DTOS
{
    public record SimulacionRequestDTO(
         int ProductoId,
         decimal Monto,
         int PlazoMeses
     );

    public record CuotaDetalleDTO2(
        int NumeroCuota,
        DateTime FechaVencimiento,
        decimal Capital,
        decimal Interes,
        decimal MontoCuota,
        decimal SaldoCapital
    );

    public record SimulacionResponseDTO(
        string ProductoNombre,
        decimal TasaAplicada,
        decimal CuotaMensual,
        List<CuotaDetalleDTO> Cronograma
    );
}

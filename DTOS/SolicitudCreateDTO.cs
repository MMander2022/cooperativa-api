namespace CooperativaApp.DTOS
{
    public record SolicitudCreateDTO(
     int SocioId,
     int ProductoId,
     decimal Monto,
     int Plazo
 );
}

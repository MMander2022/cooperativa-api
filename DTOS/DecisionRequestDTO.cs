namespace CooperativaApp.DTOS
{
    public record DecisionRequestDTO(
         int UsuarioId,
         string Comentario,
         string Accion
     );
}

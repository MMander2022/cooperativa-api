using CooperativaApp.DTOS;
using CooperativaApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CooperativaApp.Interfaces
{
    public interface IPagoService
    {
        Task<OperacionResponse> ProcesarPagoAsync(PagoRequestDTO request, int usuarioId);
    }
}

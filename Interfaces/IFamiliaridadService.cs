using CooperativaApp.DTOS;
using CooperativaApp.Models;

namespace CooperativaApp.Services.Interfaces
{
    public interface IFamiliaridadService
    {
        // 🎯 Este es el método que mencionaste, lo usaremos para el núcleo
        Task<List<FamiliaridadDTO>> GetFamiliaresBySocioAsync(int idSocioTitular);
        Task<bool> VincularFamiliarAsync(int idTitular, int idFamiliar, int idParentesco);
        Task<bool> EliminarVinculoAsync(int idFamiliaridad);
    }
}
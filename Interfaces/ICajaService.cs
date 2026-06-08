using System;
using System.Threading.Tasks;
using CooperativaApp.DTOs;

namespace CooperativaApp.Services
{
    public interface ICajaService
    {
        Task<CuadreDiarioDto> ObtenerCuadreDiarioAsync(DateTime fecha);
    }
}
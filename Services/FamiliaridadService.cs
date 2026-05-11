using CooperativaApp.Data; // 🎯 Necesario para el Contexto
using CooperativaApp.DTOS;
using CooperativaApp.Models;
using CooperativaApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CooperativaApp.Services
{
    public class FamiliaridadService : IFamiliaridadService
    {
        private readonly CooperativaContext _context;

        // 🛡️ El Constructor inyecta el contexto para que deje de salir "no existe en el contexto actual"
        public FamiliaridadService(CooperativaContext context)
        {
            _context = context;
        }

        public async Task<List<FamiliaridadDTO>> GetFamiliaresBySocioAsync(int idSocioTitular)
        {
            return await _context.Familiaridad
                .Where(f => f.IdSocioTitular == idSocioTitular && f.Activo == true)
                .Select(f => new FamiliaridadDTO
                {
                    IdFamiliaridad = f.IdFamiliaridad,
                    IdSocioFamiliar = f.IdSocioFamiliar,
                    // 🎯 Usamos las propiedades correctas de tu tabla Socios
                    NombreFamiliar = f.SocioFamiliar.Nombres + " " + f.SocioFamiliar.Apellidos,
                    NumeroDocumento = f.SocioFamiliar.DNI, // Cambiado de DniFamiliar para ser fiel a tu DTO original
                    Parentesco = f.Parentesco.Descripcion,
                    IdParentesco = f.IdParentesco
                }).ToListAsync();
        }

        public async Task<bool> VincularFamiliarAsync(int idTitular, int idFamiliar, int idParentesco)
        {
            // 🔍 BUSQUEDA TÁCTICA: ¿Existe ya una relación ACTIVA entre estos dos?
            var vinculacionExistente = await _context.Familiaridad
                .FirstOrDefaultAsync(f => f.IdSocioTitular == idTitular
                                       && f.IdSocioFamiliar == idFamiliar
                                       && f.Activo == true); // 🎯 Solo bloqueamos si está ACTIVA

            if (vinculacionExistente != null)
            {
                // Si ya existe y está activa, abortamos la misión
                return false;
            }

            // 🚀 Si llegamos aquí, o no existe o la que existe está en Activo = 0.
            // Procedemos a crear un nuevo registro limpio.
            var nuevaFamiliaridad = new Familiaridad
            {
                IdSocioTitular = idTitular,
                IdSocioFamiliar = idFamiliar,
                IdParentesco = idParentesco,
                FechaVinculacion = DateTime.Now,
                Activo = true // Nace activa
            };

            _context.Familiaridad.Add(nuevaFamiliaridad);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> EliminarVinculoAsync(int idFamiliaridad)
        {
            var vinculo = await _context.Familiaridad.FindAsync(idFamiliaridad);
            if (vinculo == null) return false;

            // Borrado Lógico Nivel Bancario
            vinculo.Activo = false;
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
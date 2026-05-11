using Microsoft.AspNetCore.Http;

namespace CooperativaApp.Interfaces
{
    public interface IFileService
    {
        // Guarda el archivo y retorna la ruta relativa para la BD
        Task<string> SaveFileAsync(IFormFile file, string subFolder);

        // Elimina archivos físicos (útil si se rechaza un aporte)
        void DeleteFile(string filePath);

        // Validación de seguridad de extensiones
        bool IsValidExtension(string fileName, string[] allowedExtensions);
    }
}
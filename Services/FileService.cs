using CooperativaApp.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace CooperativaApp.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        // Extensiones permitidas por defecto para imágenes
        private readonly string[] _imageExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };

        public FileService(IWebHostEnvironment env) => _env = env;

        public async Task<string> SaveFileAsync(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0) return string.Empty;

            // 🛡️ VALIDACIÓN DE SEGURIDAD: Solo extensiones permitidas
            if (!IsValidExtension(file.FileName, _imageExtensions))
                throw new InvalidOperationException("Tipo de archivo no permitido.");

            // Definir ruta física: C:\...\Resources\Vouchers
            string resourcesPath = Path.Combine(_env.ContentRootPath, "Resources", subFolder);

            if (!Directory.Exists(resourcesPath))
                Directory.CreateDirectory(resourcesPath);

            // Generar nombre único con GUID para evitar sobrescribir archivos
            string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            string filePath = Path.Combine(resourcesPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Retorna ruta relativa para el Front: "Resources/Vouchers/nombre.jpg"
            return Path.Combine("Resources", subFolder, fileName).Replace("\\", "/");
        }

        public void DeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            string fullPath = Path.Combine(_env.ContentRootPath, filePath);
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }

        public bool IsValidExtension(string fileName, string[] allowedExtensions)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return allowedExtensions.Contains(ext);
        }
    }
}
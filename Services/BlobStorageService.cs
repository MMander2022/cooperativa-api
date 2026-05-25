using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

public class BlobStorageService
{
    private readonly string? _connectionString;
    private readonly string? _containerName;
    private readonly BlobServiceClient? _blobServiceClient;

    public BlobStorageService(IConfiguration configuration)
    {
        // 1. Extraemos los valores de configuración de forma segura
        _connectionString = configuration.GetConnectionString("AzureBlobStorage");
        _containerName = configuration.GetValue<string>("BlobSettings:ContainerName") ?? "vouchers";

        try
        {
            // 🛡️ CONTROL DE TOLERANCIA A FALLOS DE ARRANQUE
            // Si la cadena está vacía o es genérica, evitamos inicializar el cliente para que no explote la API
            if (!string.IsNullOrEmpty(_connectionString) && !_connectionString.Contains("GITHUB") && !_connectionString.Contains("PROTEGIDA"))
            {
                _blobServiceClient = new BlobServiceClient(_connectionString);
                Console.WriteLine("🌐 [AZURE BLOB STORAGE] Cliente inicializado con éxito.");
            }
            else
            {
                Console.WriteLine("⚠️ [AZURE BLOB STORAGE] Advertencia: Cadena vacía o de desarrollo. Servicio en modo pasivo.");
            }
        }
        catch (Exception ex)
        {
            // Capturamos el error de parseo para que la API NO devuelva un Error 500 global
            Console.WriteLine($"❌ [AZURE BLOB STORAGE] Error crítico de inicialización: {ex.Message}");
            _blobServiceClient = null;
        }
    }

    public async Task<string> UploadVoucherAsync(IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0) return string.Empty;

        // Si el cliente no se pudo inicializar en el arranque, avisamos al flujo en caliente
        if (_blobServiceClient == null)
        {
            throw new Exception("El servicio de almacenamiento en la nube no está configurado correctamente en producción.");
        }

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

        // Aseguramos que el contenedor exista de forma asíncrona al momento de la carga
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        string extension = Path.GetExtension(archivo.FileName);
        string nombreUnicoArchivo = $"voucher_{Guid.NewGuid()}{extension}";

        var blobClient = containerClient.GetBlobClient(nombreUnicoArchivo);

        using (var stream = archivo.OpenReadStream())
        {
            var blobUploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = archivo.ContentType }
            };
            await blobClient.UploadAsync(stream, blobUploadOptions);
        }

        return blobClient.Uri.ToString();
    }
}
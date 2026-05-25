using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

public class BlobStorageService
{
    private readonly string _connectionString;
    private readonly string _containerName;

    public BlobStorageService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AzureBlobStorage");
        _containerName = configuration.GetValue<string>("BlobSettings:ContainerName");
    }

    public async Task<string> UploadVoucherAsync(IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0) return string.Empty;

        // 1. Conectar con el cliente de Azure Blob Storage
        var blobServiceClient = new BlobServiceClient(_connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

        // 2. Generar un nombre único de alta seguridad usando un Guid para evitar cruces
        string extension = Path.GetExtension(archivo.FileName);
        string nombreUnicoArchivo = $"voucher_{Guid.NewGuid()}{extension}";

        // 3. Obtener la referencia del Blob final en la nube
        var blobClient = containerClient.GetBlobClient(nombreUnicoArchivo);

        // 4. Transmitir el stream binario directo a Azure
        using (var stream = archivo.OpenReadStream())
        {
            var blobUploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = archivo.ContentType }
            };
            await blobClient.UploadAsync(stream, blobUploadOptions);
        }

        // 5. Retornar la URL pública absoluta lista para guardar en SQL
        return blobClient.Uri.ToString();
    }
}
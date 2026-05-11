using Microsoft.AspNetCore.Mvc;
using CooperativaApp.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace CooperativaApp.Controllers
{
    [Authorize] // 🛡️ Solo tripulación autorizada
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IFileService _fileService;
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB Límite Titanium

        public UploadController(IFileService fileService) => _fileService = fileService;

        [HttpPost("voucher")]
        public async Task<IActionResult> UploadVoucher(IFormFile file)
        {
            try
            {
                // 1. Validación de existencia
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "No se ha seleccionado ningún archivo." });

                // 2. Validación de tamaño (Seguridad contra ataques DoS)
                if (file.Length > MAX_FILE_SIZE)
                    return BadRequest(new { message = "El archivo excede el límite de 5MB." });

                // 3. Procesamiento seguro
                var path = await _fileService.SaveFileAsync(file, "Vouchers");

                return Ok(new
                {
                    dbPath = path,
                    message = "Archivo procesado y almacenado con éxito."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error interno en el hangar de archivos." });
            }
        }
    }
}
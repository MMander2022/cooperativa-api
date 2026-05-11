
using CooperativaApp.DTOS;
using CooperativaApp.Interfaces;
using CooperativaApp.Models;
using CooperativaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CooperativaApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PagosController : ControllerBase
    {
        private readonly IPagoService _pagoService;
        public PagosController(IPagoService pagoService) => _pagoService = pagoService;

        [HttpPost("procesar")]
        public async Task<IActionResult> Procesar([FromBody] PagoRequestDTO request)
        {
            var usuarioId = int.Parse(User.FindFirst("IdUsuario")?.Value ?? "0");
            var result = await _pagoService.ProcesarPagoAsync(request, usuarioId);
            return result.Exito ? Ok(result) : BadRequest(result);
        }
    }
}


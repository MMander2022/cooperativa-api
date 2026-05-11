using CooperativaApp.Data;
using CooperativaApp.DTOS;
using CooperativaApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CooperativaApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FamiliaridadController : ControllerBase
    {
        private readonly IFamiliaridadService _service;
        private readonly CooperativaContext _context;

        public FamiliaridadController(IFamiliaridadService service, CooperativaContext context)
        {
            _service = service;
            _context = context;
        }

        [HttpGet("mi-familia")]
        public async Task<IActionResult> GetMiFamilia([FromQuery] int? idSocioManual = null)
        {
            // 🎯 Normalización de Perfil: Buscamos tanto 'Admin' como 'ADMINISTRADOR'
            var perfilClaim = User.FindFirst("Perfil")?.Value?.ToUpper() ?? "";
            bool isAdmin = User.IsInRole("Admin") || User.IsInRole("ADMINISTRADOR") ||
                           perfilClaim == "ADMIN" || perfilClaim == "ADMINISTRADOR";

            int idSocioFinal;

            if (isAdmin)
            {
                if (idSocioManual.HasValue && idSocioManual > 0)
                {
                    idSocioFinal = idSocioManual.Value;
                }
                else
                {
                    // Admin sin socio seleccionado devuelve lista limpia
                    return Ok(new List<FamiliaridadDTO>());
                }
            }
            else
            {
                var idSocioClaim = User.FindFirst("IdSocio")?.Value;
                if (string.IsNullOrEmpty(idSocioClaim) || idSocioClaim == "0")
                    return Unauthorized(new { mensaje = "No se identificó un perfil de socio vinculado." });

                idSocioFinal = int.Parse(idSocioClaim);
            }

            var data = await _service.GetFamiliaresBySocioAsync(idSocioFinal);
            return Ok(data);
        }

        [HttpPost("vincular")]
        public async Task<IActionResult> Vincular([FromBody] VincularRequestDTO request)
        {
            // 🎯 Aplicamos la misma validación de Rol robusta
            var perfilClaim = User.FindFirst("Perfil")?.Value?.ToUpper() ?? "";
            bool isAdmin = User.IsInRole("Admin") || User.IsInRole("ADMINISTRADOR") ||
                           perfilClaim == "ADMIN" || perfilClaim == "ADMINISTRADOR";

            int idTitularFinal;

            if (isAdmin && request.IdSocioTitular.HasValue && request.IdSocioTitular.Value > 0)
            {
                idTitularFinal = request.IdSocioTitular.Value;
            }
            else
            {
                var claimSocio = User.FindFirst("IdSocio")?.Value;
                if (string.IsNullOrEmpty(claimSocio)) return Unauthorized();
                idTitularFinal = int.Parse(claimSocio);
            }

            var exito = await _service.VincularFamiliarAsync(idTitularFinal, request.IdFamiliar, request.IdParentesco);

            return exito
                ? Ok(new { exito = true, mensaje = "Vínculo procesado correctamente" })
                : BadRequest(new { mensaje = "No se pudo realizar la vinculación (duplicado o error de datos)" });
        }

        [HttpGet("parentescos")]
        public async Task<IActionResult> GetParentescos()
        {
            // 🎯 Blindaje contra nulos: Si Descripcion es null, evitamos que ToUpper() rompa el código
            var data = await _context.Parentescos
                .AsNoTracking()
                .Select(p => new {
                    p.IdParentesco,
                    Descripcion = (p.Descripcion ?? "SIN DEFINIR").ToUpper()
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var exito = await _service.EliminarVinculoAsync(id);
            return exito ? Ok(new { mensaje = "Vínculo revocado" }) : NotFound();
        }
    }
}
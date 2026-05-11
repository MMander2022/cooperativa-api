using CooperativaApp.Data;
using CooperativaApp.DTOS;
using CooperativaApp.Interfaces;
using CooperativaApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CooperativaApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class SettingsController : ControllerBase
    {
        private readonly CooperativaContext _context;
        private readonly ISettingsService _settingsService;

        public SettingsController(CooperativaContext context, ISettingsService settingsService)
        {
            _context = context;
            _settingsService = settingsService;
        }

        [HttpGet("public")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicConfig()
        {
            var keys = new[] { "NombreCooperativa", "MonedaPrincipal", "MantenimientoSistema" };
            var settings = await _context.GlobalSettings
                .Where(s => keys.Contains(s.SettingKey))
                .ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue);

            return Ok(settings);
        }

        [HttpGet("all-modulos")]
        [AllowAnonymous] // 🔓 AJUSTE GALÁCTICO: Acceso libre temporal para avance de módulos
        public async Task<IActionResult> GetAllModulos()
        {
            var modulos = await _context.Modulos
                .Where(m => m.Activo)
                .OrderBy(m => m.Orden)
                .ToListAsync();
            return Ok(modulos);
        }

        [HttpPost("update")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSetting([FromBody] SettingUpdateDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var success = await _settingsService.UpdateSettingAsync(dto.Key, dto.Value, int.Parse(userId));
            return success ? Ok(new { Message = "OK" }) : BadRequest();
        }
        [HttpGet("init")] // 👈 Esto es lo que el Front está buscando
        [AllowAnonymous]
        public async Task<IActionResult> GetInit()
        {
            // Obtenemos todas las configuraciones para que el Front las mapee
            var settings = await _context.GlobalSettings
                .Select(s => new { s.SettingKey, s.SettingValue })
                .ToListAsync();

            return Ok(settings);
        }
    }
}
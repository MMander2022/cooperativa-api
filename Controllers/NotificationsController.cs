// Archivo: Controllers/NotificationsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CooperativaApp.Interfaces; // 👈 IMPORTANTE
using CooperativaApp.Data;      // 👈 Donde esté tu DbContext
using System.Threading.Tasks;

namespace CooperativaApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase // 👈 Esto arregla 'Ok' y 'NotFound'
    {
        private readonly IEmailService _emailService;
        private readonly IWhatsAppService _whatsappService;
        private readonly ISmsService _smsService;
        private readonly CooperativaContext _context; // 👈 Cambia 'ApplicationDbContext' por tu nombre real

        public NotificationsController(
            IEmailService email,
            IWhatsAppService wa,
            ISmsService sms,
            CooperativaContext ctx) // 👈 Cambia aquí también
        {
            _emailService = email;
            _whatsappService = wa;
            _smsService = sms;
            _context = ctx;
        }

        [HttpPost("enviar-credenciales-activacion")]
        public async Task<IActionResult> EnviarCredenciales([FromBody] int idUsuario)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Socio)
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

            if (usuario == null || usuario.Socio == null) return NotFound();

            // Mensaje de éxito post-cambio de clave
            string mensajeExito = $@"✅ ¡Identidad Confirmada, {usuario.Socio.Nombres}! 
    Tu contraseña personal ha sido establecida correctamente.
    🛡️ Tu cuenta ahora es 100% segura. 
    ¡Ya puedes explorar todos tus beneficios en el Dashboard!";

            await Task.WhenAll(
                _emailService.SendEmailAsync(usuario.Socio.correo, "Cuenta Activada con Éxito", mensajeExito),
                _whatsappService.SendWhatsAppAsync(usuario.Socio.Telefono, mensajeExito)
            );

            return Ok();
        }
    }
}

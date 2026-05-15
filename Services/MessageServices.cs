using CooperativaApp.Interfaces;
using CooperativaApp.Models; // 👈 Asegúrate de tener aquí la clase SmtpSettings
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace CooperativaApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _settings;

        public EmailService(IOptions<SmtpSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var emailMessage = new MimeMessage();

            // 🛰️ Configuración del Remitente
            emailMessage.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));

            // 🎯 Configuración del Destinatario
            emailMessage.To.Add(new MailboxAddress("", email));

            // 📝 Asunto
            emailMessage.Subject = subject;

            // 💎 Cuerpo del mensaje (Soporta HTML para un look Pro)
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; border: 1px solid #ddd; padding: 20px; border-radius: 10px;'>
                        <h2 style='color: #004a99;'>UNIMAS</h2>
                        <hr/>
                        <p style='font-size: 1.1em;'>{message.Replace("\n", "<br/>")}</p>
                        <br/>
                        <p style='color: #888; font-size: 0.8em;'>Este es un mensaje automático, por favor no responder.</p>
                    </div>"
            };

            emailMessage.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                // 🔐 Protocolo de conexión segura (TLS)
                await client.ConnectAsync(_settings.Server, _settings.Port, SecureSocketOptions.StartTls);

                // 🔑 Autenticación con el búnker de Google
                await client.AuthenticateAsync(_settings.Username, _settings.Password);

                // 🚀 LANZAMIENTO
                await client.SendAsync(emailMessage);

                Console.WriteLine($"✅ [TITANIUM] Correo real enviado con éxito a {email}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [ERROR CRÍTICO] Fallo en el motor de correo: {ex.Message}");
                // No lanzamos la excepción para no detener el flujo del socio, 
                // pero lo dejamos registrado en los logs de la nave.
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
    }

    // 📱 Servicios de apoyo (Mantienen el log hasta que contrates proveedores reales)
    public class SmsService : ISmsService
    {
        public Task SendSmsAsync(string number, string message)
        {
            Console.WriteLine($"📱 [LOG] SMS enviado a {number}: {message}");
            return Task.CompletedTask;
        }
    }

    public class WhatsAppService : IWhatsAppService
    {
        public Task SendWhatsAppAsync(string number, string message)
        {
            Console.WriteLine($"🟢 [LOG] WhatsApp enviado a {number}: {message}");
            return Task.CompletedTask;
        }
    }
}
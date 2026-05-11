using System.Threading.Tasks;
namespace CooperativaApp.Interfaces
{
    public class IMessageServices
    {
    }
    public interface IEmailService { Task SendEmailAsync(string email, string subject, string message); }
    public interface ISmsService { Task SendSmsAsync(string number, string message); }
    public interface IWhatsAppService { Task SendWhatsAppAsync(string number, string message); }
}

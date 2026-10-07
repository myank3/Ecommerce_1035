using System.Threading.Tasks;

namespace Ecommerce_1035.Utilities.Service.IService
{
    public interface ISmsSender
    {
        ValueTask SendSmsBeeAsync(string number, string email, string message);
        ValueTask SendSmsAsync(string number, string message);
        ValueTask SendEmailAsync(string toEmail, string subject, string body);
        ValueTask SendAllChannelsAsync(
            string number,
            string email,
            string smsMessage,
            string voiceMessage,
            string emailSubject,
            string emailBody);
    }
}
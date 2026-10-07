using System.Threading.Tasks;

namespace Ecommerce_1035.Utilities.Service.IService
{
    public interface ITwilioSender
    {
        ValueTask SendSmsAsync(string number, string message);
        ValueTask SendVoiceCallAsync(string number, string message);
    }
}
using Ecommerce_1035.Utilities.Service.IService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Task = System.Threading.Tasks.Task;

namespace Ecommerce_1035.Utilities.Service
{
    public class TwilioSender : ITwilioSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<TwilioSender> _logger;

        public TwilioSender(IConfiguration config, ILogger<TwilioSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async ValueTask SendSmsAsync(string number, string message)
        {
            var (sid, token, from) = ReadTwilioConfig();
            var formattedNumber = FormatIndianNumber(number);

            TwilioClient.Init(sid, token);

            var body = message.Length > 160 ? message.Substring(0, 157) + "..." : message;

            await MessageResource.CreateAsync(
                body: body,
                from: new PhoneNumber(from),
                to: new PhoneNumber(formattedNumber));

            _logger.LogInformation("SMS sent via Twilio to {Number}", formattedNumber);
        }

        public async ValueTask SendVoiceCallAsync(string number, string message)
        {
            var (sid, token, from) = ReadTwilioConfig();
            var formattedNumber = FormatIndianNumber(number);

            TwilioClient.Init(sid, token);

            var safe = System.Security.SecurityElement.Escape(message);

            var twimlXml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
                    <Response>
                    <Say voice=""Polly.Aditi"" language=""en-IN"">{safe}</Say>
                    </Response>";

            var call = await CallResource.CreateAsync(
                to: new PhoneNumber(formattedNumber),
                from: new PhoneNumber(from),
                twiml: new Twiml(twimlXml));

            _logger.LogInformation(
                "Voice call initiated to {Number}. SID: {Sid}, Status: {Status}",
                formattedNumber, call.Sid, call.Status);
        }

        private (string sid, string token, string from) ReadTwilioConfig()
        {
            var sid = _config["SmsOptions:AccountSid"];
            var token = _config["SmsOptions:AuthToken"];
            var from = _config["SmsOptions:FromNumber"];

            if (string.IsNullOrEmpty(sid) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(from))
                throw new InvalidOperationException("Twilio options missing.");

            return (sid, token, from);
        }

        private static string FormatIndianNumber(string number)
        {
            if (string.IsNullOrWhiteSpace(number)) return number;

            number = number.Replace(" ", "").Replace("-", "").Trim();

            if (number.StartsWith("+91")) return number;
            if (number.StartsWith("0")) return "+91" + number.Substring(1);
            if (number.Length == 10 && number.All(char.IsDigit)) return "+91" + number;
            if (number.StartsWith("91") && number.Length == 12 && number.All(char.IsDigit))
                return "+" + number;

            return number;
        }
    }
}
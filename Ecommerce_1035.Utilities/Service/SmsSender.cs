using Ecommerce_1035.Utilities.Service.IService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using brevo_csharp.Api;
using brevo_csharp.Model;
using Task = System.Threading.Tasks.Task;

namespace Ecommerce_1035.Utilities.Service
{
    public class SmsSender : ISmsSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmsSender> _logger;
        private readonly HttpClient _http;
        private readonly TransactionalEmailsApi _brevo;
        private readonly string _fromEmail;
        private readonly ITwilioSender _twilio;

        public SmsSender(
            IConfiguration config,
            ILogger<SmsSender> logger,
            HttpClient http,
            ITwilioSender twilio)
        {
            _config = config;
            _logger = logger;
            _http = http;
            _twilio = twilio;

            var brevoKey = _config["Brevo:ApiKey"];
            if (string.IsNullOrEmpty(brevoKey))
                throw new InvalidOperationException("Brevo API Key is missing.");

            var brevoConfig = new brevo_csharp.Client.Configuration();
            brevoConfig.ApiKey.Add("api-key", brevoKey);
            _brevo = new TransactionalEmailsApi(brevoConfig);
            _fromEmail = _config["Brevo:FromEmail"] ?? "vibeound@gmail.com";
        }

        public async ValueTask SendSmsBeeAsync(string number, string email, string message)
        {
            var formattedNumber = FormatIndianNumber(number);
            var code = ExtractCode(message);

            var tasks = new[]
            {
                SafeAsync(() => SendTextBeeAsync(formattedNumber, message), "TextBee SMS"),
                SafeAsync(() => SendBrevoAsync(email, "Login Code",
                    $"<p>Your login code is: <strong>{code}</strong></p>"), "Brevo Email")
            };

            await Task.WhenAll(tasks);
        }

        public async ValueTask SendSmsAsync(string number, string message)
        {
            await _twilio.SendSmsAsync(number, message);
        }

        public ValueTask SendEmailAsync(string toEmail, string subject, string body)
            => SendBrevoAsync(toEmail, subject, body);

        public async ValueTask SendAllChannelsAsync(
            string number,
            string email,
            string smsMessage,
            string voiceMessage,
            string emailSubject,
            string emailBody)
        {
            var formattedNumber = FormatIndianNumber(number);

            var tasks = new[]
            {
                SafeAsync(() => SendTextBeeAsync(formattedNumber, smsMessage), "TextBee SMS"),
                SafeAsync(() => _twilio.SendSmsAsync(formattedNumber, smsMessage), "Twilio SMS"),
                SafeAsync(() => _twilio.SendVoiceCallAsync(formattedNumber, voiceMessage), "Twilio Voice"),
                SafeAsync(() => SendBrevoAsync(email, emailSubject, emailBody), "Brevo Email"),
            };

            await Task.WhenAll(tasks);
        }

        private async ValueTask SendTextBeeAsync(string number, string message)
        {
            var apiKey = _config["TextBee:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("TextBee API key missing.");

            var json = $"{{\"recipients\":[\"{number}\"],\"message\":\"{EscapeJson(message)}\"}}";

            using var request = new HttpRequestMessage(HttpMethod.Post,
                "https://api.textbee.dev/api/v1/gateway/send-sms");
            request.Headers.Add("x-api-key", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"textbee failed: {await response.Content.ReadAsStringAsync()}");

            _logger.LogInformation("SMS sent via textbee to {Number}", number);
        }

        private async ValueTask SendBrevoAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrEmpty(toEmail)) return;

            var email = new SendSmtpEmail
            {
                To = new System.Collections.Generic.List<SendSmtpEmailTo>
                {
                    new SendSmtpEmailTo(toEmail)
                },
                Subject = subject,
                HtmlContent = body,
                Sender = new SendSmtpEmailSender("Book-Shelf", _fromEmail)
            };

            var result = await _brevo.SendTransacEmailAsync(email);
            _logger.LogInformation("Email sent to {Email}. MessageId: {Id}", toEmail, result.MessageId);
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

        private async Task SafeAsync(Func<ValueTask> action, string channel)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{Channel} failed", channel);
            }
        }

        private static string ExtractCode(string message)
        {
            for (int i = message.Length - 6; i >= 0; i--)
            {
                var slice = message.Substring(i, 6);
                if (slice.All(char.IsDigit)) return slice;
            }
            return "000000";
        }

        private static string EscapeJson(string text) =>
            text.Replace("\\", "\\\\").Replace("\"", "\\\"")
                .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }
}
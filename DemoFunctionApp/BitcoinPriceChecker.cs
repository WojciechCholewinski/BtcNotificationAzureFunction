using System.Net;
using System.Net.Mail;
using System.Text.Json;
using DemoFunctionApp.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;

namespace DemoFunctionApp
{
    public class BitcoinPriceChecker
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _symbol;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _fromEmail;
        private readonly string _fromDisplayName;
        private readonly string _fromPassword;
        private readonly string _toEmail;

        public BitcoinPriceChecker(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;

            // Wczytywanie konfiguracji
            _apiKey = _configuration["Finnhub:ApiKey"];
            _symbol = _configuration["Finnhub:Symbol"];
            _smtpHost = _configuration["EmailSettings:SmtpHost"];
            _smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
            _fromEmail = _configuration["EmailSettings:FromEmail"];
            _fromDisplayName = _configuration["EmailSettings:FromDisplayName"];
            _fromPassword = _configuration["EmailSettings:FromPassword"];
            _toEmail = _configuration["EmailSettings:ToEmail"];
        }

        [Function("BitcoinPriceChecker")]
        public async Task Run([TimerTrigger("0 0 9 * * *")] TimerInfo myTimer)
        {
            string url = $"https://finnhub.io/api/v1/quote?symbol={_symbol}&token={_apiKey}";

            try
            {
                // Pobieranie danych z API
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };
                var quote = JsonSerializer.Deserialize<BitcoinQuote>(responseBody, options);

                if (quote.CurrentPrice > 90000)
                {
                    string email = $"Current Bitcoin Price: ${quote.CurrentPrice}$," +
                                   $" Change: {quote.Change}$ ({quote.PercentChange}%)." +
                                   $" Today's Range: ${quote.LowPrice} - ${quote.HighPrice}";

                    await SendEmailAsync(email);
                }

            }
            catch (Exception e)
            {
                string email = $"Wystapil Exception: {e.Message}";

                await SendEmailAsync(email);
            }
        }

        private async Task SendEmailAsync(string emailBody)
        {
            using MailMessage message = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromDisplayName),
                Subject = "Cena Bitcoina",
                Body = emailBody
            };

            message.To.Add(_toEmail);

            using SmtpClient smtp = new SmtpClient
            {
                Host = _smtpHost,
                Port = _smtpPort,
                Credentials = new NetworkCredential(_fromEmail, _fromPassword),
                EnableSsl = true
            };

            await smtp.SendMailAsync(message);
        }
    }
}
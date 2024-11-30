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
        private readonly string _apiKey;
        private readonly string _symbol;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _fromEmail;
        private readonly string _fromPassword;
        private readonly string _toEmail;
        public BitcoinPriceChecker(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient(nameof(BitcoinPriceChecker));

            // Wczytywanie konfiguracji
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Wartosci z pliku konfiguracyjnego lub zmiennych srodowiskowych
            _apiKey = configuration["Finnhub:ApiKey"] ?? Environment.GetEnvironmentVariable("FINNHUB_API_KEY");
            _symbol = configuration["Finnhub:Symbol"];
            _smtpHost = configuration["EmailSettings:SmtpHost"];
            _smtpPort = int.Parse(configuration["EmailSettings:SmtpPort"]);
            _fromEmail = configuration["EmailSettings:FromEmail"];
            _fromPassword = configuration["EmailSettings:FromPassword"] ?? Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
            _toEmail = configuration["EmailSettings:ToEmail"];
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
                var quote = JsonSerializer.Deserialize<BitcoinQuote>(responseBody);

                if (quote.CurrentPrice > 65000)
                {
                    string email = $"Current Bitcoin Price: ${quote.CurrentPrice}$," +
                                   $" Change: {quote.Change}$ ({quote.PercentChange}%)." +
                                   $" Today's Range: ${quote.LowPrice} - ${quote.HighPrice}";

                    await SendEmailAsync(email);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        private async Task SendEmailAsync(string emailBody)
        {
            MailMessage message = new MailMessage
            {
                From = new MailAddress(_fromEmail),
                Subject = "Cena bitcoina",
                Body = emailBody
            };

            message.To.Add(_toEmail);

            using var smtp = new SmtpClient
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
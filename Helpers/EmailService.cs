using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace LibraryAccounting.Helpers
{
    public class EmailService
    {
        private readonly MailSettings _settings;

        public EmailService(MailSettings settings)
        {
            _settings = settings;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string body)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(_settings.DisplayName, _settings.Email));
                email.To.Add(new MailboxAddress(toName, toEmail));
                email.Subject = subject;

                var builder = new BodyBuilder();
                builder.HtmlBody = body;
                email.Body = builder.ToMessageBody();

                using (var smtp = new SmtpClient())
                {
                    // Подключаемся к серверу
                    await smtp.ConnectAsync(_settings.SmtpServer, _settings.Port, SecureSocketOptions.StartTls);

                    // Аутентификация
                    await smtp.AuthenticateAsync(_settings.Email, _settings.Password);

                    // Отправка
                    await smtp.SendAsync(email);

                    // Отключаемся
                    await smtp.DisconnectAsync(true);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmailService ошибка: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"EmailService stack: {ex.StackTrace}");
                return false;
            }
        }
    }
}
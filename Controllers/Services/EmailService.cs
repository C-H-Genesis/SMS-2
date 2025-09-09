using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Options;
using Models;

namespace EmailAuth
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
                if (string.IsNullOrWhiteSpace(toEmail))
                    throw new ArgumentNullException(nameof(toEmail));
                if (string.IsNullOrWhiteSpace(subject))
                    throw new ArgumentNullException(nameof(subject));
                if (string.IsNullOrWhiteSpace(body))
                    throw new ArgumentNullException(nameof(body));


            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_emailSettings.From));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart("plain")
            {
                Text = body ?? "[No content provided]"
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_emailSettings.From, _emailSettings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using PMHUB.Application.DTOs;
using PMHUB.Application.IServices;
using System.Reflection;

namespace PMHUB.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly string _templatesFolder;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
            _templatesFolder = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory,
                "Email",
                "Templates");
        }

        public async Task SendConfirmationEmailAsync(
            string toEmail, string firstName, string confirmationLink)
        {
            var subject = "Confirmez votre email PMHub";
            var body = await LoadTemplateAsync(
                "confirmation-email.html",
                ("FirstName", firstName),
                ("ConfirmationLink", confirmationLink));

            await SendEmailAsync(toEmail, subject, body);
        }

 
        public async Task SendApprovalEmailAsync(string toEmail, string firstName)
        {
            var subject = "Votre compte PMHub a été approuvé !";
            var body = await LoadTemplateAsync(
                "approval-email.html",
                ("FirstName", firstName));

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendRejectionEmailAsync(string toEmail, string firstName)
        {
            var subject = "Votre demande d'accès PMHub a été refusée";
            var body = await LoadTemplateAsync(
                "rejection-email.html",
                ("FirstName", firstName));

            await SendEmailAsync(toEmail, subject, body);
        }

        private async Task<string> LoadTemplateAsync(string fileName, params (string Key, string Value)[] values)
        {
            var templatePath = Path.Combine(_templatesFolder, fileName);
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Email template not found: {templatePath}");

            var html = await File.ReadAllTextAsync(templatePath);
            foreach (var (key, value) in values)
            {
                html = html.Replace($"{{{{{key}}}}}", value ?? string.Empty, StringComparison.Ordinal);
            }

            return html;
        }

        // ── HELPER 
        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_settings.From));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_settings.Host, _settings.Port,
                SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}
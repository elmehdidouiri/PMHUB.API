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

        public async Task SendPasswordResetCodeAsync(
            string toEmail,
            string firstName,
            string code,
            int expiresInMinutes)
        {
            var subject = "PMHub password reset verification code";
            var body = await LoadTemplateAsync(
                "password-reset-code.html",
                ("FirstName", firstName),
                ("VerificationCode", code),
                ("ExpiresInMinutes", expiresInMinutes.ToString()));

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendHoursAllocationReminderAsync(
            string toEmail,
            string firstName,
            decimal currentHours)
        {
            var subject = "PMHub - rappel de saisie des heures";
            var safeFirstName = string.IsNullOrWhiteSpace(firstName) ? "à vous" : firstName;
            var body = $"""
                <p>Bonjour {safeFirstName},</p>
                <p>Ceci est un rappel automatique pour renseigner vos heures allouées au travail dans PMHub.</p>
                <p>Total saisi sur la période sélectionnée: <strong>{currentHours:0.##} heures</strong>.</p>
                <p>Merci de mettre vos heures à jour dès que possible.</p>
                """;

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendHoursAllocationReminderAsync(
            string toEmail,
            string firstName,
            decimal currentHours,
            decimal expectedHours,
            decimal missingHours)
        {
            var subject = "PMHub - rappel de saisie des heures";
            var safeFirstName = string.IsNullOrWhiteSpace(firstName) ? "à vous" : firstName;
            var body = $"""
                <p>Bonjour {safeFirstName},</p>
                <p>Ceci est un rappel automatique car vos heures de la semaine précédente ne sont pas complètes dans PMHub.</p>
                <p>Total saisi: <strong>{currentHours:0.##} heures</strong>.</p>
                <p>Total attendu: <strong>{expectedHours:0.##} heures</strong>.</p>
                <p>Heures restantes à renseigner: <strong>{missingHours:0.##} heures</strong>.</p>
                <p>Merci de mettre vos heures à jour dès que possible.</p>
                """;

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendSupervisorVisitReminderAsync(string toEmail, string firstName)
        {
            var subject = "PMHub - merci de contacter votre superviseur";
            var safeFirstName = string.IsNullOrWhiteSpace(firstName) ? "à vous" : firstName;
            var body = $"""
                <p>Bonjour {safeFirstName},</p>
                <p>Nous avons constaté qu'aucune heure n'a été bookée sur PMHub depuis au moins un mois.</p>
                <p>Merci de passer voir votre superviseur afin de régulariser votre situation et vos allocations d'heures.</p>
                <p>Cordialement,</p>
                <p>L'équipe PMHub</p>
                """;

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

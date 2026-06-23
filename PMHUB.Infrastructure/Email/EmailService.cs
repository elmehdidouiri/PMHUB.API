using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using PMHUB.Application.DTOs;
using PMHUB.Application.IServices;

namespace PMHUB.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly IEmailTemplateRenderer _templateRenderer;

        public EmailService(
            IOptions<EmailSettings> settings,
            IEmailTemplateRenderer templateRenderer)
        {
            _settings = settings.Value;
            _templateRenderer = templateRenderer;
        }

        public async Task SendConfirmationEmailAsync(
            string toEmail, string firstName, string confirmationLink)
        {
            var subject = "Confirmez votre email PMHub";
            var body = await _templateRenderer.RenderCorporateEmailAsync(
                "confirmation-email.html",
                "Confirmation de votre adresse email",
                "Finalisez votre inscription PMHub en confirmant votre adresse email.",
                new Dictionary<string, string?>
                {
                    ["FirstName"] = firstName,
                    ["ConfirmationLink"] = confirmationLink
                });

            await SendEmailAsync(toEmail, subject, body);
        }

 
        public async Task SendApprovalEmailAsync(string toEmail, string firstName)
        {
            var subject = "Votre compte PMHub a été approuvé !";
            var body = await _templateRenderer.RenderCorporateEmailAsync(
                "approval-email.html",
                "Compte PMHub approuvé",
                "Votre accès à la plateforme PMHub est maintenant actif.",
                new Dictionary<string, string?>
                {
                    ["FirstName"] = firstName
                });

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendRejectionEmailAsync(string toEmail, string firstName)
        {
            var subject = "Votre demande d'accès PMHub a été refusée";
            var body = await _templateRenderer.RenderCorporateEmailAsync(
                "rejection-email.html",
                "Demande d'accès PMHub",
                "Mise à jour concernant votre demande d'accès à PMHub.",
                new Dictionary<string, string?>
                {
                    ["FirstName"] = firstName
                });

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPasswordResetCodeAsync(
            string toEmail,
            string firstName,
            string code,
            int expiresInMinutes)
        {
            var subject = "PMHub password reset verification code";
            var body = await _templateRenderer.RenderCorporateEmailAsync(
                "password-reset-code.html",
                "Password reset verification",
                "Use this verification code to continue your PMHub password reset.",
                new Dictionary<string, string?>
                {
                    ["FirstName"] = firstName,
                    ["VerificationCode"] = code,
                    ["ExpiresInMinutes"] = expiresInMinutes.ToString()
                });

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendHoursAllocationReminderAsync(
            string toEmail,
            string firstName,
            decimal currentHours)
        {
            var subject = "PMHub - rappel de saisie des heures";
            var safeFirstName = string.IsNullOrWhiteSpace(firstName) ? "à vous" : firstName;
            var body = await _templateRenderer.RenderCorporateEmailAsync(
                "hours-allocation-reminder.html",
                "Rappel de saisie des heures",
                "Une mise à jour de vos heures PMHub est attendue.",
                new Dictionary<string, string?>
                {
                    ["FirstName"] = safeFirstName,
                    ["CurrentHours"] = FormatHours(currentHours)
                });

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
            var body = await _templateRenderer.RenderCorporateEmailAsync(
                "hours-allocation-target-reminder.html",
                "Rappel de saisie des heures",
                "Vos heures de la semaine précédente ne sont pas encore complètes.",
                new Dictionary<string, string?>
                {
                    ["FirstName"] = safeFirstName,
                    ["CurrentHours"] = FormatHours(currentHours),
                    ["ExpectedHours"] = FormatHours(expectedHours),
                    ["MissingHours"] = FormatHours(missingHours)
                });

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendSupervisorVisitReminderAsync(string toEmail, string firstName)
        {
            var subject = "PMHub TE - action requise sur le booking des heures";
            var safeFirstName = string.IsNullOrWhiteSpace(firstName) ? "à vous" : firstName;
            var body = await _templateRenderer.RenderCorporateEmailAsync(
                "supervisor-visit-reminder.html",
                "Action requise sur le booking des heures",
                "Aucune heure n'a été bookée depuis au moins un mois.",
                new Dictionary<string, string?>
                {
                    ["FirstName"] = safeFirstName
                });

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendInternBookingReminderToSupervisorAsync(
            string toEmail,
            string supervisorFirstName,
            string internName,
            int daysWithoutBooking)
        {
            var subject = "PMHub TE - suivi des heures intern";
            var safeFirstName = string.IsNullOrWhiteSpace(supervisorFirstName) ? "Superviseur" : supervisorFirstName;
            var safeInternName = string.IsNullOrWhiteSpace(internName) ? "l'intern concerné" : internName;
            var body = await _templateRenderer.RenderCorporateEmailAsync(
                "intern-booking-reminder-supervisor.html",
                "Suivi des heures intern",
                "Un booking d'heures intern nécessite votre attention.",
                new Dictionary<string, string?>
                {
                    ["FirstName"] = safeFirstName,
                    ["InternName"] = safeInternName,
                    ["DaysWithoutBooking"] = daysWithoutBooking.ToString()
                });

            await SendEmailAsync(toEmail, subject, body);
        }

        private static string FormatHours(decimal hours) => $"{hours:0.##}";

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_settings.From));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_settings.Host, _settings.Port,
                SecureSocketOptions.Auto);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}

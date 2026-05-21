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
            var content = await LoadTemplateAsync(
                "confirmation-email.html",
                ("FirstName", firstName),
                ("ConfirmationLink", confirmationLink));
            var body = BuildCorporateEmail(
                "Confirmation de votre adresse email",
                "Finalisez votre inscription PMHub en confirmant votre adresse email.",
                content);

            await SendEmailAsync(toEmail, subject, body);
        }

 
        public async Task SendApprovalEmailAsync(string toEmail, string firstName)
        {
            var subject = "Votre compte PMHub a été approuvé !";
            var content = await LoadTemplateAsync(
                "approval-email.html",
                ("FirstName", firstName));
            var body = BuildCorporateEmail(
                "Compte PMHub approuvé",
                "Votre accès à la plateforme PMHub est maintenant actif.",
                content);

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendRejectionEmailAsync(string toEmail, string firstName)
        {
            var subject = "Votre demande d'accès PMHub a été refusée";
            var content = await LoadTemplateAsync(
                "rejection-email.html",
                ("FirstName", firstName));
            var body = BuildCorporateEmail(
                "Demande d'accès PMHub",
                "Mise à jour concernant votre demande d'accès à PMHub.",
                content);

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPasswordResetCodeAsync(
            string toEmail,
            string firstName,
            string code,
            int expiresInMinutes)
        {
            var subject = "PMHub password reset verification code";
            var content = await LoadTemplateAsync(
                "password-reset-code.html",
                ("FirstName", firstName),
                ("VerificationCode", code),
                ("ExpiresInMinutes", expiresInMinutes.ToString()));
            var body = BuildCorporateEmail(
                "Password reset verification",
                "Use this verification code to continue your PMHub password reset.",
                content);

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendHoursAllocationReminderAsync(
            string toEmail,
            string firstName,
            decimal currentHours)
        {
            var subject = "PMHub - rappel de saisie des heures";
            var safeFirstName = string.IsNullOrWhiteSpace(firstName) ? "à vous" : firstName;
            var content = $"""
                <p style="margin:0 0 16px;">Bonjour {safeFirstName},</p>
                <p style="margin:0 0 16px;">Ceci est un rappel automatique pour renseigner vos heures allouées au travail dans PMHub.</p>
                <div style="margin:22px 0;padding:16px 18px;background:#f8fafc;border-left:4px solid #E98300;border-radius:4px;">
                    <span style="display:block;color:#52616b;font-size:13px;margin-bottom:4px;">Total saisi sur la période sélectionnée</span>
                    <strong style="font-size:20px;color:#2E4957;">{currentHours:0.##} heures</strong>
                </div>
                <p style="margin:0;">Merci de mettre vos heures à jour dès que possible.</p>
                """;
            var body = BuildCorporateEmail(
                "Rappel de saisie des heures",
                "Une mise à jour de vos heures PMHub est attendue.",
                content);

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
            var content = $"""
                <p style="margin:0 0 16px;">Bonjour {safeFirstName},</p>
                <p style="margin:0 0 16px;">Ceci est un rappel automatique car vos heures de la semaine précédente ne sont pas complètes dans PMHub.</p>
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="margin:22px 0;border-collapse:collapse;">
                    <tr>
                        <td style="padding:14px;background:#f8fafc;border:1px solid #e2e8f0;">
                            <span style="display:block;color:#52616b;font-size:12px;">Total saisi</span>
                            <strong style="color:#2E4957;font-size:18px;">{currentHours:0.##} h</strong>
                        </td>
                        <td style="padding:14px;background:#f8fafc;border:1px solid #e2e8f0;">
                            <span style="display:block;color:#52616b;font-size:12px;">Total attendu</span>
                            <strong style="color:#2E4957;font-size:18px;">{expectedHours:0.##} h</strong>
                        </td>
                        <td style="padding:14px;background:#fff7ed;border:1px solid #fed7aa;">
                            <span style="display:block;color:#9a3412;font-size:12px;">Restant</span>
                            <strong style="color:#E98300;font-size:18px;">{missingHours:0.##} h</strong>
                        </td>
                    </tr>
                </table>
                <p style="margin:0;">Merci de mettre vos heures à jour dès que possible.</p>
                """;
            var body = BuildCorporateEmail(
                "Rappel de saisie des heures",
                "Vos heures de la semaine précédente ne sont pas encore complètes.",
                content);

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendSupervisorVisitReminderAsync(string toEmail, string firstName)
        {
            var subject = "PMHub TE - action requise sur le booking des heures";
            var safeFirstName = string.IsNullOrWhiteSpace(firstName) ? "à vous" : firstName;
            var content = $"""
                <p style="margin:0 0 16px;">Bonjour {safeFirstName},</p>
                <p style="margin:0 0 16px;">Dans le cadre du suivi PMHub de TE Connectivity, nous avons constaté qu'aucune heure n'a été bookée depuis au moins un mois.</p>
                <p style="margin:0 0 16px;">Merci de régulariser les allocations d'heures dans PMHub dès que possible.</p>
                <p style="margin:24px 0 0;color:#52616b;">Cordialement,<br /><strong style="color:#2E4957;">PMHub - TE Connectivity</strong></p>
                """;
            var body = BuildCorporateEmail(
                "Action requise sur le booking des heures",
                "Aucune heure n'a été bookée depuis au moins un mois.",
                content);

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
            var content = $"""
                <p style="margin:0 0 16px;">Bonjour {safeFirstName},</p>
                <p style="margin:0 0 16px;">Dans le cadre du suivi PMHub de TE Connectivity, aucune heure n'a été bookée pour <strong>{safeInternName}</strong> depuis au moins <strong>{daysWithoutBooking}</strong> jours.</p>
                <p style="margin:0 0 16px;">Comme les heures des interns doivent être suivies par leur superviseur, merci de vérifier et régulariser les bookings nécessaires dans PMHub.</p>
                <p style="margin:24px 0 0;color:#52616b;">Cordialement,<br /><strong style="color:#2E4957;">PMHub - TE Connectivity</strong></p>
                """;
            var body = BuildCorporateEmail(
                "Suivi des heures intern",
                "Un booking d'heures intern nécessite votre attention.",
                content);

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

        private static string BuildCorporateEmail(string title, string preheader, string content)
        {
            const string teLogoUrl = "https://www.te.com/_TEincludes/ver/1696/v2/images/te-connectivity-logo.png";

            return $"""
                <!DOCTYPE html>
                <html lang="fr">
                <head>
                    <meta charset="utf-8" />
                    <meta name="viewport" content="width=device-width, initial-scale=1" />
                    <title>{title}</title>
                </head>
                <body style="margin:0;padding:0;background:#edf1f3;font-family:Arial,Helvetica,sans-serif;color:#1f2933;">
                    <div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;">
                        {preheader}
                    </div>
                    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#edf1f3;margin:0;padding:28px 0;">
                        <tr>
                            <td align="center" style="padding:0 14px;">
                                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:640px;background:#ffffff;border:1px solid #d9e2ec;border-radius:8px;overflow:hidden;">
                                    <tr>
                                        <td style="height:6px;background:#E98300;font-size:0;line-height:0;">&nbsp;</td>
                                    </tr>
                                    <tr>
                                        <td style="padding:24px 30px 20px;background:#2E4957;">
                                            <img src="{teLogoUrl}" width="172" alt="TE Connectivity" style="display:block;border:0;max-width:172px;height:auto;margin-bottom:18px;" />
                                            <div style="font-size:12px;line-height:1.4;letter-spacing:1.4px;text-transform:uppercase;color:#A4D4E6;font-weight:700;">PMHub Notification</div>
                                            <h1 style="margin:8px 0 0;font-size:24px;line-height:1.3;color:#ffffff;font-weight:700;letter-spacing:0;">{title}</h1>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style="padding:30px;font-size:15px;line-height:1.65;color:#1f2933;">
                                            {content}
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style="padding:20px 30px;background:#f8fafc;border-top:1px solid #e2e8f0;">
                                            <p style="margin:0 0 8px;font-size:13px;line-height:1.5;color:#52616b;">
                                                Cet email automatique a été envoyé par PMHub pour le suivi opérationnel TE Connectivity.
                                            </p>
                                            <p style="margin:0;font-size:12px;line-height:1.5;color:#7b8794;">
                                                Merci de ne pas répondre directement à ce message. Si vous avez besoin d'aide, veuillez contacter l'administrateur PMHub.
                                            </p>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>
                """;
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

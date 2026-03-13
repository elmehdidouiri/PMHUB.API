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

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

         public async Task SendConfirmationEmailAsync(
            string toEmail, string firstName, string confirmationLink)
        {
            var subject = "Confirmez votre email PMHub";
            var body = $@"
                <h2>Bonjour {firstName},</h2>
                <p>Merci pour votre inscription sur <strong>PMHub</strong> !</p>
                <p>Cliquez sur le bouton ci-dessous pour confirmer votre adresse email :</p>
                <br/>
                <a href='{confirmationLink}'
                   style='background-color:#4CAF50;
                          color:white;
                          padding:12px 24px;
                          text-decoration:none;
                          border-radius:4px;
                          display:inline-block;'>
                    Confirmer mon email
                </a>
                <br/><br/>
                <p>Ce lien expire dans <strong>24 heures</strong>.</p>
                <p>Si vous n'avez pas créé de compte, ignorez cet email.</p>
                <br/>
                <p>Cordialement,</p>
                <p><strong>L'équipe PMHub</strong></p>";

            await SendEmailAsync(toEmail, subject, body);
        }

 
        public async Task SendApprovalEmailAsync(string toEmail, string firstName)
        {
            var subject = "Votre compte PMHub a été approuvé !";
            var body = $@"
                <h2>Bonjour {firstName},</h2>
                <p>Bonne nouvelle ! Votre compte PMHub a été 
                   <strong>approuvé</strong> par l'administrateur.</p>
                <p>Vous pouvez maintenant vous connecter et accéder à la plateforme.</p>
                <br/>
                <a href='https://pmhub.com/login'
                   style='background-color:#4CAF50;
                          color:white;
                          padding:12px 24px;
                          text-decoration:none;
                          border-radius:4px;
                          display:inline-block;'>
                    Se connecter
                </a>
                <br/><br/>
                <p>Cordialement,</p>
                <p><strong>L'équipe PMHub</strong></p>";

            await SendEmailAsync(toEmail, subject, body);
        }

         public async Task SendRejectionEmailAsync(string toEmail, string firstName)
        {
            var subject = "Votre demande d'accès PMHub a été refusée";
            var body = $@"
                <h2>Bonjour {firstName},</h2>
                <p>Votre demande d'accès à la plateforme PMHub a été 
                   <strong>refusée</strong> par l'administrateur.</p>
                <p>Pour plus d'informations, veuillez contacter l'administrateur.</p>
                <br/>
                <p>Cordialement,</p>
                <p><strong>L'équipe PMHub</strong></p>";

            await SendEmailAsync(toEmail, subject, body);
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
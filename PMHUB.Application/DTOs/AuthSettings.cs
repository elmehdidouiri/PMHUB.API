namespace PMHUB.Application.DTOs
{
    public class AuthSettings
    {
        public string RefreshTokenType { get; set; } = "refresh_token";
        public int PasswordResetCodeExpirationMinutes { get; set; } = 10;
        public int PasswordResetTokenExpirationMinutes { get; set; } = 10;
        public int MaxPasswordResetAttempts { get; set; } = 5;
    }
}

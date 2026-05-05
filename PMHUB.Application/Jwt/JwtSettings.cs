namespace PMHUB.Application.Jwt
{
    /// <summary>
    /// Options liees a la section JwtSettings (appsettings).
    /// </summary>
    public class JwtSettings
    {
        public string Secret { get; set; } = string.Empty;

        public int ExpirationMinutes { get; set; } = 30;

        public int RefreshTokenExpirationHours { get; set; } = 24;

        public string Issuer { get; set; } = "PMHub";

        public string Audience { get; set; } = "PMHubClients";
    }
}
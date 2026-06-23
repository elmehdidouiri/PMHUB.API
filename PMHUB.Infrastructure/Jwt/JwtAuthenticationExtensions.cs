using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PMHUB.Application.Exceptions;
using PMHUB.Application.Jwt;
using System.Text;

namespace PMHUB.Infrastructure.Jwt
{
    public static class JwtAuthenticationExtensions
    {
        public static IServiceCollection AddPmHubJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var section = configuration.GetSection("JwtSettings");
            services.Configure<JwtSettings>(section);

            var jwt = section.Get<JwtSettings>()
                ?? throw new InvalidOperationException("JwtSettings section is missing or invalid.");

            if (string.IsNullOrWhiteSpace(jwt.Secret))
            {
                throw new InvalidOperationException("JwtSettings:Secret is required.");
            }

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwt.Issuer,
                        ValidAudience = jwt.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret))
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = async context =>
                        {
                            context.HandleResponse();
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = "application/json";

                            var response = ApiResponse.Fail(
                                "Your session has expired or is invalid. Please sign in again.");
                            await context.Response.WriteAsJsonAsync(response);
                        },
                        OnForbidden = async context =>
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            context.Response.ContentType = "application/json";

                            var response = ApiResponse.Fail("Not authorised.");
                            await context.Response.WriteAsJsonAsync(response);
                        },
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            if (!string.IsNullOrWhiteSpace(accessToken) &&
                                path.StartsWithSegments("/hubs/admin-notifications"))
                            {
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            return services;
        }
    }
}

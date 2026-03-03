using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
 using PMHUB.Application.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using ValidationException = PMHUB.Application.Exceptions.ValidationException;

namespace PMHUB.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception interceptée : {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = exception switch
            {
                NotFoundException ex => (
                    StatusCode: (int)HttpStatusCode.NotFound,
                    Body: ApiResponse.Fail(ex.Message)
                ),
                BadRequestException ex => (
                    StatusCode: (int)HttpStatusCode.BadRequest,
                    Body: ApiResponse.Fail(ex.Message)
                ),
                ConflictException ex => (
                    StatusCode: (int)HttpStatusCode.Conflict,
                    Body: ApiResponse.Fail(ex.Message)
                ),
                ForbiddenException ex => (
                    StatusCode: (int)HttpStatusCode.Forbidden,
                    Body: ApiResponse.Fail(ex.Message)
                ),
                UnauthorizedException ex => (
                    StatusCode: (int)HttpStatusCode.Unauthorized,
                    Body: ApiResponse.Fail(ex.Message)
                ),
                ValidationException ex => (
                    StatusCode: (int)HttpStatusCode.UnprocessableEntity,
                    Body: ApiResponse.Fail(ex.Message, ex.Errors)
                ),
                _ => (
                    StatusCode: (int)HttpStatusCode.InternalServerError,
                    Body: ApiResponse.Fail("Une erreur interne du serveur s'est produite.")
                )
            };

            context.Response.StatusCode = response.StatusCode;

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response.Body, options));
        }
    }
}
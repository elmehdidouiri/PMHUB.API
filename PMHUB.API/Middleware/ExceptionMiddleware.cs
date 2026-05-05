using PMHUB.API.Helpers;
using PMHUB.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;
using ValidationException = PMHUB.Application.Exceptions.ValidationException;

namespace PMHUB.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Intercepted exception: {Message}", ex.Message);
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
                    Body: ApiResponse.Fail(ErrorMessageTranslator.Translate(ex.Message))
                ),
                BadRequestException ex => (
                    StatusCode: (int)HttpStatusCode.BadRequest,
                    Body: ApiResponse.Fail(ErrorMessageTranslator.Translate(ex.Message))
                ),
                ConflictException ex => (
                    StatusCode: (int)HttpStatusCode.Conflict,
                    Body: ApiResponse.Fail(ErrorMessageTranslator.Translate(ex.Message))
                ),
                ForbiddenException ex => (
                    StatusCode: (int)HttpStatusCode.Forbidden,
                    Body: ApiResponse.Fail(ErrorMessageTranslator.Translate(ex.Message))
                ),
                UnauthorizedException ex => (
                    StatusCode: (int)HttpStatusCode.Unauthorized,
                    Body: ApiResponse.Fail(ErrorMessageTranslator.Translate(ex.Message))
                ),
                ValidationException ex => (
                    StatusCode: (int)HttpStatusCode.UnprocessableEntity,
                    Body: ApiResponse.Fail(
                        ErrorMessageTranslator.BuildValidationSummary(ex.Errors, ErrorMessageTranslator.Translate(ex.Message)),
                        ErrorMessageTranslator.Translate(ex.Errors))
                ),
                JsonException ex => (
                    StatusCode: (int)HttpStatusCode.BadRequest,
                    Body: ApiResponse.Fail("Invalid JSON format: " + ex.Message)
                ),
                BadHttpRequestException ex => (
                    StatusCode: (int)HttpStatusCode.BadRequest,
                    Body: ApiResponse.Fail("Malformed request: " + ex.Message)
                ),
                DbUpdateConcurrencyException => (
                    StatusCode: (int)HttpStatusCode.Conflict,
                    Body: ApiResponse.Fail("The resource was modified by another operation. Refresh and retry.")
                ),
                _ => (
                    StatusCode: (int)HttpStatusCode.InternalServerError,
                    Body: ApiResponse.Fail(_env.IsDevelopment() 
                        ? "An unexpected error occurred: " + exception.Message 
                        : "An internal server error occurred. Please contact support.")
                )
            };

            context.Response.StatusCode = response.StatusCode;

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response.Body, options));
        }
    }
}

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PMHUB.API.BackgroundServices;
using PMHUB.API.Helpers;
using PMHUB.API.Middleware;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IRepositories;
using PMHUB.Application.Interfaces;
using PMHUB.Application.IServices;
using PMHUB.Application.Services;
using PMHUB.Application.Services.Implementation;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Jwt;
using PMHUB.Infrastructure.Persistence;
using PMHUB.Infrastructure.Repositories;
using PMHUB.Infrastructure.Repositories.Generique;
using PMHUB.Infrastructure.Repositories.Implementation;
using PMHUB.Infrastructure.Services;
using PMHUB.Infrastructure.Storage;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine(BCrypt.Net.BCrypt.HashPassword("user123"));

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("../logs/pmhub-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var requestBodyType = ValidationMetadataHelper.GetRequestBodyType(context);
        var errors = ValidationMetadataHelper.NormalizeErrors(context.ModelState);

        return new BadRequestObjectResult(
            new
            {
                Success = false,
                Message = ErrorMessageTranslator.BuildValidationSummary(errors, "Validation failed."),
                Errors = errors,
                RequiredFields = ValidationMetadataHelper.GetRequiredFields(requestBodyType),
                RequiredFieldGroups = ValidationMetadataHelper.GetRequiredFieldGroups(requestBodyType)
            });
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", corsBuilder =>
    {
        corsBuilder.WithOrigins("http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PMHub API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<PMHubDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsAssembly("PMHUB.Infrastructure")));

builder.Services.AddPmHubJwtAuthentication(builder.Configuration);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("isAdmin", "true"));

    options.AddPolicy("UserOnly", policy =>
        policy.RequireClaim("isAdmin", "false"));

    options.AddPolicy("ApprovedUser", policy =>
        policy.RequireAuthenticatedUser());
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IHourEntryRepository, HourEntryRepository>();
builder.Services.AddScoped<IProjectFileRepository, ProjectFileRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
builder.Services.AddScoped<IHoursAllocationDashboardRepository, HoursAllocationDashboardRepository>();
builder.Services.AddScoped<IInternAllocationRepository, InternAllocationRepository>();

builder.Services.AddScoped<IRepository<UserHourlyRate>, Repository<UserHourlyRate>>();
builder.Services.AddScoped<IRepository<Holiday>, Repository<Holiday>>();

builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IProjectFileService, ProjectFileService>();
builder.Services.AddScoped<IInternService, InternService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IBusinessUnitService, BusinessUnitService>();
builder.Services.AddScoped<IPlantService, PlantService>();
builder.Services.AddScoped<ITechnologyService, TechnologyService>();
builder.Services.AddScoped<ISolutionDomainService, SolutionDomainService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IHourEntryService, HourEntryService>();
builder.Services.AddScoped<IHourSummaryService, HourSummaryService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IHoursAllocationDashboardService, HoursAllocationDashboardService>();
builder.Services.AddScoped<IInternStatisticsService, InternStatisticsService>();
builder.Services.AddScoped<IHourBookingReminderService, HourBookingReminderService>();
builder.Services.AddScoped<IHeaderNotificationService, HeaderNotificationService>();
builder.Services.AddHostedService<WeeklyHourBookingReminderHostedService>();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 52428800;
});

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("Email"));

builder.Services.Configure<AuthSettings>(
    builder.Configuration.GetSection("AuthSettings"));

builder.Services.Configure<PMHUB.Shared.Models.CompanyStandards>(
    builder.Configuration.GetSection("CompanyStandards"));

builder.Services.Configure<HourBookingReminderSettings>(
    builder.Configuration.GetSection("HourBookingReminders"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PMHubDbContext>();
    dbContext.Database.Migrate();
}

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Log.Information("PMHUB application started");
app.Run();

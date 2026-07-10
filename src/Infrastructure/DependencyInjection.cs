// Layer: Infrastructure
// Responsibility: Registers infrastructure persistence, external services, options, and authentication.
using System.Security.Claims;
using System.Text;
using Greenlab.Application.Common;
using Greenlab.Application.Common.Interfaces;
using Greenlab.Domain.Enums;
using Greenlab.Domain.Interfaces;
using Greenlab.Infrastructure.Persistence;
using Greenlab.Infrastructure.Persistence.Repositories;
using Greenlab.Infrastructure.Services;
using Greenlab.Infrastructure.Services.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Greenlab.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddMemoryCache();

        services.Configure<JwtOptions>(options => ConfigureJwt(options, configuration));
        services.Configure<DeviceJwtOptions>(options => ConfigureDeviceJwt(options, configuration));
        services.Configure<CrowdVisionOptions>(options => ConfigureCrowdVision(options, configuration));
        services.Configure<StorageOptions>(options => options.ScheduleUploadsPath = configuration[$"{StorageOptions.SectionName}:ScheduleUploadsPath"] ?? options.ScheduleUploadsPath);
        services.Configure<ScheduleVisionOptions>(options => ConfigureScheduleVision(options, configuration));
        services.Configure<ImportedUserOptions>(options => options.DefaultPassword = configuration[$"{ImportedUserOptions.SectionName}:DefaultPassword"] ?? options.DefaultPassword);

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILabRepository, LabRepository>();
        services.AddScoped<ILabDeviceRepository, LabDeviceRepository>();
        services.AddScoped<ILabSensorRepository, LabSensorRepository>();
        services.AddScoped<ILabControlOverrideRepository, LabControlOverrideRepository>();
        services.AddScoped<ILabSectionScheduleRepository, LabSectionScheduleRepository>();
        services.AddScoped<IStaffLabAssignmentRepository, StaffLabAssignmentRepository>();
        services.AddScoped<ILabChangeRequestRepository, LabChangeRequestRepository>();
        services.AddScoped<IScheduleImageImportRepository, ScheduleImageImportRepository>();
        services.AddScoped<IManualControlCommandRepository, ManualControlCommandRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        services.AddSingleton<IDeviceKeyHasher, DeviceKeyHasher>();
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IDeviceTokenService, DeviceTokenService>();
        services.AddSingleton<ILegacyDeviceStateService, LegacyDeviceStateService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddSingleton<IImportedUserDefaults, ImportedUserDefaults>();

        services.AddScoped<ICrowdCountClient>(serviceProvider =>
            ActivatorUtilities.CreateInstance<CrowdCountClient>(serviceProvider, new HttpClient { Timeout = TimeSpan.FromSeconds(20) }));
        services.AddScoped<IScheduleImageAnalysisService>(serviceProvider =>
            ActivatorUtilities.CreateInstance<OpenAiScheduleImageAnalysisService>(serviceProvider, new HttpClient { Timeout = TimeSpan.FromMinutes(3) }));

        services.AddHostedService<OccupancyPollingService>();

        AddJwtAuthentication(services, configuration);
        return services;
    }

    private static void AddJwtAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwt = new JwtOptions();
        ConfigureJwt(jwt, configuration);

        var deviceJwt = new DeviceJwtOptions();
        ConfigureDeviceJwt(deviceJwt, configuration);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };
            })
            .AddJwtBearer(DeviceAuthConstants.Scheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = deviceJwt.Issuer,
                    ValidAudience = deviceJwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(deviceJwt.Key)),
                    ClockSkew = TimeSpan.FromSeconds(deviceJwt.ClockSkewSeconds),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = DeviceAuthConstants.DeviceIdClaim
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(Role.Admin)));
            options.AddPolicy("StaffOnly", policy => policy.RequireRole(nameof(Role.Teacher), nameof(Role.TeacherAssistant)));
            options.AddPolicy(DeviceAuthConstants.Policy, policy =>
            {
                policy.AddAuthenticationSchemes(DeviceAuthConstants.Scheme);
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(DeviceAuthConstants.DeviceIdClaim);
                policy.RequireClaim(DeviceAuthConstants.LabIdClaim);
            });
        });
    }

    private static void ConfigureJwt(JwtOptions options, IConfiguration configuration)
    {
        options.Issuer = configuration[$"{JwtOptions.SectionName}:Issuer"] ?? options.Issuer;
        options.Audience = configuration[$"{JwtOptions.SectionName}:Audience"] ?? options.Audience;
        options.Key = configuration[$"{JwtOptions.SectionName}:Key"] ?? options.Key;
        if (int.TryParse(configuration[$"{JwtOptions.SectionName}:ExpireMinutes"], out var expireMinutes))
            options.ExpireMinutes = expireMinutes;
    }

    private static void ConfigureDeviceJwt(DeviceJwtOptions options, IConfiguration configuration)
    {
        options.Issuer = configuration[$"{DeviceJwtOptions.SectionName}:Issuer"] ?? options.Issuer;
        options.Audience = configuration[$"{DeviceJwtOptions.SectionName}:Audience"] ?? options.Audience;
        options.Key = configuration[$"{DeviceJwtOptions.SectionName}:Key"] ?? options.Key;
        if (int.TryParse(configuration[$"{DeviceJwtOptions.SectionName}:ExpireMinutes"], out var expireMinutes))
            options.ExpireMinutes = expireMinutes;
        if (int.TryParse(configuration[$"{DeviceJwtOptions.SectionName}:ClockSkewSeconds"], out var clockSkewSeconds))
            options.ClockSkewSeconds = clockSkewSeconds;
    }

    private static void ConfigureCrowdVision(CrowdVisionOptions options, IConfiguration configuration)
    {
        options.BaseUrl = configuration[$"{CrowdVisionOptions.SectionName}:BaseUrl"] ?? options.BaseUrl;
        options.ApiKey = configuration[$"{CrowdVisionOptions.SectionName}:ApiKey"];
        if (bool.TryParse(configuration[$"{CrowdVisionOptions.SectionName}:UseStubWhenUnavailable"], out var useStub))
            options.UseStubWhenUnavailable = useStub;
        if (int.TryParse(configuration[$"{CrowdVisionOptions.SectionName}:StubCount"], out var stubCount))
            options.StubCount = stubCount;
        if (int.TryParse(configuration[$"{CrowdVisionOptions.SectionName}:CacheSeconds"], out var cacheSeconds))
            options.CacheSeconds = cacheSeconds;
    }

    private static void ConfigureScheduleVision(ScheduleVisionOptions options, IConfiguration configuration)
    {
        options.ApiKey = configuration[$"{ScheduleVisionOptions.SectionName}:ApiKey"] ?? options.ApiKey;
        options.BaseUrl = configuration[$"{ScheduleVisionOptions.SectionName}:BaseUrl"] ?? options.BaseUrl;
        options.Model = configuration[$"{ScheduleVisionOptions.SectionName}:Model"] ?? options.Model;
        if (int.TryParse(configuration[$"{ScheduleVisionOptions.SectionName}:MaxTokens"], out var maxTokens))
            options.MaxTokens = maxTokens;
    }
}

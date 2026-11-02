// Layer: WebAPI
// Responsibility: Configures the HTTP host, middleware pipeline, and layer dependency injection.
using System.Text.Json.Serialization;
using Greenlab.Application;
using Greenlab.Application.Common;
using Greenlab.Application.Common.Interfaces;
using Greenlab.Infrastructure;
using Greenlab.Infrastructure.Persistence;
using Greenlab.WebAPI.Middleware;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Greenlab API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT: enter `Bearer {token}`",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
ScheduleClock.Configure(builder.Configuration["Schedule:TimeZoneId"]);

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "DevFrontend",
        policy =>
            policy
                .WithOrigins("http://localhost:4200", "https://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwords = scope.ServiceProvider.GetRequiredService<IPasswordHashService>();
    var deviceKeyHasher = scope.ServiceProvider.GetRequiredService<IDeviceKeyHasher>();
    await DbSeeder.SeedAsync(db, passwords, deviceKeyHasher);
    await DbSeeder.EnsureDemoDashboardAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("DevFrontend");
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

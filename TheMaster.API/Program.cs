using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TheMaster.API.Middleware;
using TheMaster.Application.Common;
using TheMaster.Application.Interfaces;
using TheMaster.Infrastructure.Data;
using TheMaster.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. DAPPER & DATABASE CONNECTION FACTORY (SQL Server 2025 / themasterdb)
builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();

// 2. DEPENDENCY INJECTION FOR SERVICES
builder.Services.AddHttpClient<ISslCommerzService, SslCommerzService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ITestimonialService, TestimonialService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// 3. JWT AUTHENTICATION
var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? "TheMasterEnterpriseUltraSecureSuperSecretKey20261121!#@$";
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "TheMasterAPI",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "TheMasterClients",
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 4. CORS (Allows Angular Admin & Website & Flutter Apps)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 5. CONTROLLERS & JSON FORMATTING
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Enforce Global Unified ApiResponse format on all ModelState validation errors
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value != null && e.Value.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(x => !string.IsNullOrWhiteSpace(x.ErrorMessage) ? x.ErrorMessage : "Invalid field value."))
                .ToList();

            var message = errors.Count > 0 
                ? string.Join(" ", errors) 
                : "Validation failed for the submitted data.";

            var response = ApiResponse<object>.Fail(message, 400, errors);
            return new BadRequestObjectResult(response);
        };
    });

// 6. SWAGGER / OPENAPI WITH JWT AUTHORIZATION BUTTON
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "The Master Enterprise API (Dapper Powered)",
        Version = "v1",
        Description = "High-Performance REST API with Dapper micro-ORM & Global Unified Response"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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

// Ensure wwwroot directory exists for static file uploads
var webRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(webRootPath)) Directory.CreateDirectory(webRootPath);

var app = builder.Build();

// 7. GLOBAL EXCEPTION HANDLING MIDDLEWARE (Returns unified ApiResponse on any error)
app.UseMiddleware<GlobalExceptionMiddleware>();

// 8. AUTO-INITIALIZE DATABASE AND SEED DATA (VIA DAPPER)
await DbInitializer.InitializeAsync(app.Services);

// 9. SWAGGER - ALWAYS ENABLED FOR CONVENIENCE (At /swagger and /)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "The Master Enterprise API v1");
    c.RoutePrefix = "swagger";
});

// Redirect root to /swagger for instant access
app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseCors("AllowAll");

var contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".webp"] = "image/webp";
contentTypeProvider.Mappings[".apk"] = "application/vnd.android.package-archive";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 10. NOTIFY CONSOLE ON STARTUP
app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\n=======================================================");
    Console.WriteLine("  🚀 THE MASTER ENTERPRISE API IS ONLINE!");
    Console.WriteLine("  🌐 Swagger UI: http://localhost:5000/swagger");
    Console.WriteLine("  🌐 Root URL  : http://localhost:5000");
    Console.WriteLine("  📊 Database  : SQL Server (themasterdb via Dapper)");
    Console.WriteLine("  🔑 Admin Auth: abc@bd.com / 123");
    Console.WriteLine("=======================================================\n");
    Console.ResetColor();
});

app.Run();

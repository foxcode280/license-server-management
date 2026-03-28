using LicenseManager.API.Data;
using LicenseManager.API.Helpers;
using LicenseManager.API.Repositories;
using LicenseManager.API.Repositories.Interfaces;
using LicenseManager.API.Services;
using LicenseManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, ".keys");
Directory.CreateDirectory(dataProtectionPath);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("MetronuxLicenseManager");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Metronux License Server API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

builder.Services.AddScoped<DbConnectionFactory>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<IUserManagementRepository, UserManagementRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductFeatureRepository, ProductFeatureRepository>();
builder.Services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IDeviceOsTypeRepository, DeviceOsTypeRepository>();
builder.Services.AddScoped<IOfflineActivationRepository, OfflineActivationRepository>();
builder.Services.AddScoped<RefreshTokenRepository>();
builder.Services.AddScoped<LoginHistoryRepository>();
builder.Services.AddScoped<ILicenseRepository, LicenseRepository>();
builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductFeatureService, ProductFeatureService>();
builder.Services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IDeviceOsTypeService, DeviceOsTypeService>();
builder.Services.AddScoped<IOfflineActivationService, OfflineActivationService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<LicenseProtectionService>();
builder.Services.AddScoped<SystemMachineInfoService>();

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new Exception("Jwt:Key missing in configuration.");
}

if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new Exception("Jwt:Issuer missing in configuration.");
}

if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new Exception("Jwt:Audience missing in configuration.");
}

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
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey))
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"JWT authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            if (!string.IsNullOrWhiteSpace(context.ErrorDescription))
            {
                context.Response.Headers["x-auth-error"] = context.ErrorDescription;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

if (builder.Environment.IsDevelopment())
{
    // Allow the Vite dev server to call this API during local development.
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("LocalDevCors", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:3100",
                    "http://127.0.0.1:3100")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });
}

var app = builder.Build();

Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"JWT Issuer: {jwtIssuer}");
Console.WriteLine($"JWT Audience: {jwtAudience}");

//await EnsureDatabaseCompatibilityAsync(app.Configuration);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("LocalDevCors");
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

static async Task EnsureDatabaseCompatibilityAsync(IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return;
    }

    await using var connection = new NpgsqlConnection(connectionString + ";Include Error Detail=true");
    await connection.OpenAsync();

    const string sql = @"
ALTER TABLE IF EXISTS public.subscriptions
    ADD COLUMN IF NOT EXISTS status_description text;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = ''public''
          AND table_name = ''offline_activation_requests'') THEN
        CREATE UNIQUE INDEX IF NOT EXISTS ux_offline_activation_requests_license_id
            ON public.offline_activation_requests (license_id);
    END IF;
END
$$;

CREATE OR REPLACE FUNCTION public.sp_get_device_os_types()
RETURNS TABLE(
    id bigint,
    os_name character varying,
    description text,
    status character varying
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT dot.id,
           dot.os_name::character varying,
           dot.description::text,
           CASE
               WHEN dot.status IS NULL THEN 'ACTIVE'
               WHEN dot.status::text IN ('1', 'true', 'TRUE', 'active', 'ACTIVE') THEN 'ACTIVE'
               WHEN dot.status::text IN ('0', 'false', 'FALSE', 'inactive', 'INACTIVE') THEN 'INACTIVE'
               ELSE dot.status::text
           END::character varying
    FROM public.device_os_types dot
    ORDER BY dot.id;
END;
$$;";

    await using var command = new NpgsqlCommand(sql, connection);
    await command.ExecuteNonQueryAsync();
}


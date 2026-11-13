using Azure.Communication.Email;
using Duett.API.Data;
using Duett.API.Models;
using Duett.API.Repositories;
using Duett.API.Repositories.IRepositories;
using Duett.API.Services;
using Duett.API.Services.IServices;
using Duett.API.Storage;
using Duett.API.Storage.IStorage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.AzureAppServices;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Skicka applikationsloggar till Azure App Service.
builder.Logging.AddAzureWebAppDiagnostics();

builder.Logging.AddFilter(
    "Duett.API.Services.AzureEmailService",
    LogLevel.Information);

builder.Logging.AddFilter(
    "Microsoft.AspNetCore.Diagnostics",
    LogLevel.Error);

builder.Services.Configure<AzureFileLoggerOptions>(options =>
{
    options.FileName = "duett-diagnostics-";
    options.FileSizeLimit = 1024 * 1024;
    options.RetainedFileCountLimit = 3;
});

// Krävs för Identitys e-postverifiering och lösenordsåterställning.
builder.Services.AddDataProtection();

// Databas.
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection saknas.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity för användare och roller.
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.SignIn.RequireConfirmedEmail = true;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Azure Communication Services för riktiga mejl.
var emailConnectionString =
    builder.Configuration["Email:ConnectionString"];

var emailSenderAddress =
    builder.Configuration["Email:SenderAddress"];

var emailFrontendUrl =
    builder.Configuration["Email:FrontendUrl"];

if (string.IsNullOrWhiteSpace(emailConnectionString))
{
    throw new InvalidOperationException(
        "Email:ConnectionString saknas.");
}

if (string.IsNullOrWhiteSpace(emailSenderAddress))
{
    throw new InvalidOperationException(
        "Email:SenderAddress saknas.");
}

if (string.IsNullOrWhiteSpace(emailFrontendUrl) ||
    !Uri.TryCreate(
        emailFrontendUrl,
        UriKind.Absolute,
        out var frontendUri) ||
    (frontendUri.Scheme != Uri.UriSchemeHttps &&
     !(frontendUri.Scheme == Uri.UriSchemeHttp &&
       frontendUri.IsLoopback)) ||
    !string.IsNullOrEmpty(frontendUri.Query) ||
    !string.IsNullOrEmpty(frontendUri.Fragment))
{
    throw new InvalidOperationException(
        "Email:FrontendUrl måste vara en giltig frontendadress " +
        "utan query eller fragment. HTTPS krävs utom för localhost.");
}

builder.Services.AddSingleton(
    new EmailClient(emailConnectionString));

builder.Services.AddScoped<IEmailService, AzureEmailService>();

// Tjänster.
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddScoped<
    IUserProfileService,
    UserProfileService>();

builder.Services.AddScoped<
    IPartnerPreferencesService,
    PartnerPreferencesService>();

builder.Services.AddScoped<IMatchService, MatchService>();

builder.Services.AddScoped<
    IProfileLikeService,
    ProfileLikeService>();

builder.Services.AddScoped<
    IUserMatchService,
    UserMatchService>();

builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IChatService, ChatService>();

builder.Services.AddScoped<
    IMatchQuestionService,
    MatchQuestionService>();

builder.Services.AddScoped<
    IProfilePhotoProcessor,
    ProfilePhotoProcessor>();

builder.Services.AddScoped<
    IProfilePhotoService,
    ProfilePhotoService>();

// Privat bildlagring.
builder.Services.AddScoped<
    IPrivatePhotoStorage,
    LocalPrivatePhotoStorage>();

// Repositories.
builder.Services.AddScoped<
    IUserProfileRepository,
    UserProfileRepository>();

builder.Services.AddScoped<
    IPartnerPreferencesRepository,
    PartnerPreferencesRepository>();

builder.Services.AddScoped<IMatchRepository, MatchRepository>();

builder.Services.AddScoped<
    IProfileLikeRepository,
    ProfileLikeRepository>();

builder.Services.AddScoped<
    IUserMatchRepository,
    UserMatchRepository>();

builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();

builder.Services.AddScoped<
    IMatchQuestionRepository,
    MatchQuestionRepository>();

builder.Services.AddScoped<
    IProfilePhotoRepository,
    ProfilePhotoRepository>();

// JWT-inställningar.
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey) ||
    string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "Jwt:Key, Jwt:Issuer och Jwt:Audience måste anges.");
}

// Samma Base64-avkodning som i TokenService.
byte[] keyBytes;

try
{
    keyBytes = Convert.FromBase64String(jwtKey);
}
catch (FormatException)
{
    throw new InvalidOperationException(
        "Jwt:Key måste vara en giltig Base64-sträng.");
}

if (keyBytes.Length < 32)
{
    throw new InvalidOperationException(
        "JWT-nyckeln måste innehålla minst 32 byte.");
}

// Validera JWT från klienten.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,

            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

            ValidAlgorithms = new[]
            {
                SecurityAlgorithms.HmacSha256
            },

            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role,

            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Generiska felsvar vid oväntade fel.
builder.Services.AddProblemDetails();

// Tillåt anrop från den konfigurerade frontendens origin.
var frontendOrigin =
    frontendUri.GetLeftPart(UriPartial.Authority);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(frontendOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Begränsa förfrågningar till lösenordsåterställningen.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddPolicy("PasswordRecovery", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey:
                context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var app = builder.Build();

// Logga oväntade undantag och returnera ett generiskt felsvar.
app.UseExceptionHandler();

// Hantera HTTP-anrop som klienten har avbrutit.
app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (OperationCanceledException)
        when (context.RequestAborted.IsCancellationRequested)
    {
        app.Logger.LogDebug(
            "Klienten avbröt anropet till {Path}.",
            context.Request.Path);
    }
});

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

// API-dokumentation.
if (app.Environment.IsDevelopment() ||
    app.Configuration.GetValue<bool>("ApiDocs:Enabled"))
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();

// Skapa roller om de saknas.
await IdentitySeeder.SeedRolesAsync(app.Services);

// Lägg till spelfrågor i utvecklingsmiljön.
if (app.Environment.IsDevelopment())
{
    await GameQuestionSeeder.SeedAsync(app.Services);
}

// Bekräfta i loggen att versionen med diagnostik har startat.
app.Logger.LogWarning(
    "Duett har startat med Azure-diagnostik och AzureEmailService.");

app.Run();
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
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Krävs för Identitys e-postverifiering och lösenordsåterställning.
builder.Services.AddDataProtection();

// Databasanslutning från konfigurationen.
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

// Tjänster.
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, DevelopmentEmailService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<
    IPartnerPreferencesService,
    PartnerPreferencesService>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<IProfileLikeService, ProfileLikeService>();
builder.Services.AddScoped<IUserMatchService, UserMatchService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IMatchQuestionService, MatchQuestionService>();
builder.Services.AddScoped<IProfilePhotoProcessor, ProfilePhotoProcessor>();
builder.Services.AddScoped<IProfilePhotoService, ProfilePhotoService>();

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
builder.Services.AddScoped<IProfileLikeRepository, ProfileLikeRepository>();
builder.Services.AddScoped<IUserMatchRepository, UserMatchRepository>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<
    IMatchQuestionRepository,
    MatchQuestionRepository>();
builder.Services.AddScoped<
    IProfilePhotoRepository,
    ProfilePhotoRepository>();

// JWT-inställningar från konfigurationen, exempelvis User Secrets.
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

// Samma avkodning som i TokenService.
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

// Validera token som skickas till API:et.
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

// Tillåt anrop från React-appen.
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
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
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var app = builder.Build();



// Hantera avbrutna HTTP-anrop.
// TaskCanceledException ärver från OperationCanceledException.
app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (OperationCanceledException)
        when (context.RequestAborted.IsCancellationRequested)
    {
        // Klienten har avbrutit anropet.
        // Försök inte skriva något svar till den.
        app.Logger.LogDebug(
            "Klienten avbröt anropet till {Path}.",
            context.Request.Path);
    }
});
if (app.Environment.IsDevelopment() ||
    builder.Configuration.GetValue<bool>("ApiDocs:Enabled"))
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseRouting();

// CORS ligger före autentisering och behörighetskontroll.
app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

// Skapa roller om de saknas.
await IdentitySeeder.SeedRolesAsync(app.Services);

// Lägg till spelfrågor i utvecklingsmiljön.
if (app.Environment.IsDevelopment())
{
    await GameQuestionSeeder.SeedAsync(app.Services);
}

app.Run();
using System;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using SOLTIUS_Web_API_Add_On.Authentication;
using SOLTIUS_Web_API_Add_On.Database.Factories;
using SOLTIUS_Web_API_Add_On.Database.Initializers;
using SOLTIUS_Web_API_Add_On.Database.Interfaces;
using SOLTIUS_Web_API_Add_On.Middleware;
using SOLTIUS_Web_API_Add_On.Models.Configuration;
using SOLTIUS_Web_API_Add_On.Repositories;
using SOLTIUS_Web_API_Add_On.Services;
using SOLTIUS_Web_API_Add_On.Services.AuditLog;
using SOLTIUS_Web_API_Add_On.Services.Configuration;
using SOLTIUS_Web_API_Add_On.Services.Status;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// P0: Kestrel limits
// ============================================================
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1_048_576; // 1MB
});

builder.Services.AddControllers();

// ============================================================
// P0: Startup validation — reject if signing key is default
// ============================================================
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtOptions>(jwtSection);
JwtOptions jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

// 1. Resolve JWT_SIGNING_KEY dari env variable jika tersedia
string? envSigningKey = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY");
if (!string.IsNullOrWhiteSpace(envSigningKey))
{
    jwtOptions.SigningKey = envSigningKey;
}

if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) ||
    jwtOptions.SigningKey.StartsWith("${") ||
    jwtOptions.SigningKey.Length < 32 ||
    jwtOptions.SigningKey.Contains("BABIBABIBABIBABIBABIBABIBABIBABIBABIBABI"))
{
    if (builder.Environment.IsDevelopment())
    {
        jwtOptions.SigningKey = "SoltiusAddonSecretKey2026Min32CharactersLong!";
    }
    else
    {
        throw new InvalidOperationException(
            "JWT SigningKey is not configured. " +
            "Set environment variable JWT_SIGNING_KEY to a random string (min 32 chars).");
    }
}

// 2. Validate & resolve client secrets
string? envClientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
if (jwtOptions.Clients != null)
{
    foreach (var client in jwtOptions.Clients)
    {
        if (!string.IsNullOrWhiteSpace(envClientSecret))
        {
            client.ClientSecret = envClientSecret;
        }

        if (string.IsNullOrWhiteSpace(client.ClientSecret) ||
            client.ClientSecret.StartsWith("${") ||
            client.ClientSecret.Contains("BABIBABIBABIBABIBABIBABIBABIBABIBABIBABI"))
        {
            if (builder.Environment.IsDevelopment())
            {
                client.ClientSecret = "SoltiusClientSecretSchedulerAddon2026!";
            }
            else
            {
                throw new InvalidOperationException(
                    $"Client '{client.ClientId}' has a default ClientSecret. " +
                    $"Set environment variable CLIENT_SECRET before starting.");
            }
        }
    }
}

// Pastikan JwtOptions di DI terupdate dengan key & secret yang telah di-resolve
builder.Services.PostConfigure<JwtOptions>(options =>
{
    options.SigningKey = jwtOptions.SigningKey;
    options.Issuer = jwtOptions.Issuer;
    options.Audience = jwtOptions.Audience;
    options.AccessTokenMinutes = jwtOptions.AccessTokenMinutes;
    options.RefreshTokenDays = jwtOptions.RefreshTokenDays;
    options.Clients = jwtOptions.Clients;
});

byte[] jwtSigningKey = System.Text.Encoding.UTF8.GetBytes(jwtOptions.SigningKey);

// ============================================================
// JWT Auth
// ============================================================
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtSigningKey),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var authHeader = ctx.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader))
                {
                    string raw = authHeader.Trim();
                    while (raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        raw = raw.Substring(7).Trim();
                    }
                    if (!string.IsNullOrEmpty(raw))
                    {
                        ctx.Token = raw;
                    }
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"[JWT Auth Failed]: {ctx.Exception.Message}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ============================================================
// Refresh Token Store (singleton, in-memory)
// ============================================================
int refreshDays = jwtOptions.RefreshTokenDays > 0 ? jwtOptions.RefreshTokenDays : 7;
builder.Services.AddSingleton(new RefreshTokenStore(refreshDays * 24 * 60));

// ============================================================
// P1: Rate Limiting (built-in .NET 8)
// ============================================================
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddFixedWindowLimiter("api-limiter", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
});

// ============================================================
// P2: Health Checks (built-in)
// ============================================================
builder.Services.AddHealthChecks();

// ============================================================
// P1: Audit Log — Channel<T> + BackgroundService
// ============================================================
builder.Services.AddSingleton(Channel.CreateBounded<ApiLogEntry>(new BoundedChannelOptions(10000)
{
    FullMode = BoundedChannelFullMode.DropOldest
}));
builder.Services.AddSingleton(Channel.CreateBounded<SyncLogEntry>(new BoundedChannelOptions(10000)
{
    FullMode = BoundedChannelFullMode.DropOldest
}));
builder.Services.AddSingleton<IAuditLogService, AuditLogService>();

// ============================================================
// P1: Log Flush Worker (reads from Channel, writes to DB log)
// ============================================================
string logConnStr = "";
try
{
    var logDbSection = builder.Configuration.GetSection("LogDatabase");
    var logDb = logDbSection.Get<LogDatabaseConfig>();

    string logServer = Environment.GetEnvironmentVariable("LOG_DB_SERVER") ?? logDb?.Server ?? "";
    string logDbName = Environment.GetEnvironmentVariable("LOG_DB_NAME") ?? logDb?.DatabaseName ?? "";
    string logUser = Environment.GetEnvironmentVariable("LOG_DB_USER") ?? logDb?.UserName ?? "";
    string logPass = Environment.GetEnvironmentVariable("LOG_DB_PASS") ?? logDb?.Password ?? "";

    if (logServer.StartsWith("${") || string.IsNullOrWhiteSpace(logServer))
    {
        if (builder.Environment.IsDevelopment())
        {
            logServer = "localhost";
            logDbName = "NYANKYO";
            logUser = "sa";
            logPass = "P@ssw0rd";
        }
    }

    if (!string.IsNullOrWhiteSpace(logServer) && !string.IsNullOrWhiteSpace(logDbName) && !logServer.StartsWith("${"))
    {
        logConnStr = $"Server={logServer};" +
            (logDb != null && logDb.Port > 0 ? $"Port={logDb.Port};" : "") +
            $"Database={logDbName};" +
            $"User Id={logUser};Password={logPass};" +
            $"TrustServerCertificate=true;";
    }
}
catch { }

if (!string.IsNullOrWhiteSpace(logConnStr))
{
    builder.Services.AddSingleton(new LogDatabaseOptions { ConnectionString = logConnStr });
    builder.Services.AddHostedService<LogFlushWorker>();
}

// ============================================================
// Application Services
// ============================================================
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IStatusService, StatusService>();
builder.Services.AddSingleton<IDatabaseConnectionFactory, DatabaseConnectionFactory>();
builder.Services.AddSingleton<IDatabaseInitializerFactory, DatabaseInitializerFactory>();

builder.Services.AddTransient<MySqlDatabaseInitializer>();
builder.Services.AddTransient<SqlServerDatabaseInitializer>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();

// ============================================================
// Swagger (P0: only in Development)
// ============================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "Input JWT Bearer token only."
    });

    options.AddSecurityRequirement((doc) => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

// ============================================================
// Build
// ============================================================
var app = builder.Build();

// ============================================================
// Startup DB init (staging)
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var cfgService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();
    if (cfgService.ConfigExists())
    {
        try
        {
            var dbConfig = cfgService.GetDatabaseConfig();
            var factory = scope.ServiceProvider.GetRequiredService<IDatabaseInitializerFactory>();
            var initializer = factory.Create(dbConfig);
            await initializer.InitializeAsync(dbConfig);
        }
        catch { }
    }
}

// ============================================================
// Startup Log DB init
// ============================================================
if (!string.IsNullOrWhiteSpace(logConnStr))
{
    try
    {
        var logInitializer = new LogDatabaseInitializer(logConnStr);
        await logInitializer.InitializeAsync();
    }
    catch { }
}

// ============================================================
// Middleware pipeline (order matters)
// ============================================================
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// P1: Rate limiting
app.UseRateLimiter();

// P2: Health check
app.MapHealthChecks("/health");

app.MapControllers();

// P0: Swagger only in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();

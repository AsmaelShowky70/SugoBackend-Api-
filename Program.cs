using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SugoBackend.Data;
using SugoBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// IMPORTANT: On some hosts (Railway, Heroku, etc.) the developer HTTPS certificate
// is not available. These platforms typically terminate TLS at a proxy and provide
// a PORT environment variable for HTTP only. To avoid Kestrel failing when an
// HTTPS URL is present but no certificate is configured, prefer binding to HTTP
// on the provided PORT unless a certificate is explicitly configured.
{
    var aspnetcoreUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    var certPath = builder.Configuration["Kestrel:Certificates:Default:Path"]
                   ?? Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Path");
    var portEnv = Environment.GetEnvironmentVariable("PORT");

    // If the platform provided a PORT env (typical on Railway, Heroku, etc.) and no
    // Kestrel certificate is configured, force the app to bind to HTTP on that port.
    // This ensures the runtime listens where the platform's proxy expects it and
    // avoids 502/HTTPS configuration errors when a cert isn't available.
    if (!string.IsNullOrEmpty(portEnv) && string.IsNullOrEmpty(certPath))
    {
        builder.WebHost.UseUrls($"http://*:{portEnv}");
    }
    else if (!string.IsNullOrEmpty(aspnetcoreUrls) && aspnetcoreUrls.IndexOf("https", StringComparison.OrdinalIgnoreCase) >= 0 && string.IsNullOrEmpty(certPath))
    {
        // Back-compat: if ASPNETCORE_URLS explicitly contained https but there's no cert,
        // fall back to the platform port if present, otherwise default to 5000.
        var p = portEnv ?? "5000";
        builder.WebHost.UseUrls($"http://*:{p}");
    }
}

#region Services Configuration

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication & Authorization
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
{
    throw new InvalidOperationException("JWT configuration is missing in appsettings.json");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Sugo Backend API",
        Version = "v1.0",
        Description = "Simplified Sugo app backend (chat/social task app)",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Sugo Team"
        }
    });

    // Add JWT Bearer authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token in the text input below.\n\nExample: \"Bearer eyJhbGciOiJIUzI1NiIs...\""
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// Custom Services
builder.Services.AddScoped<ITokenService, TokenService>();

#endregion

var app = builder.Build();

// Track whether startup tasks (like DB migration) succeeded so health checks can report accurately
var startupOk = false;

#region Middleware Configuration

// Apply migrations and seed database (wrapped in try/catch so the app can start and
// report useful logs instead of crashing the process in PaaS environments).
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();
        startupOk = true;
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        // Log the exception and keep the app running. On platforms like Railway,
        // this prevents the host from reporting a generic "failed to respond" and
        // gives us actionable logs.
        logger.LogError(ex, "Failed to apply database migrations during startup.");
        startupOk = false;
    }
}

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sugo Backend API v1.0");
    c.RoutePrefix = "swagger";
});


// app.UseHttpsRedirection();

// CORS
app.UseCors("AllowAll");

// Honor proxy headers (X-Forwarded-For, X-Forwarded-Proto) when running behind
// a reverse proxy / PaaS (Railway, Heroku, etc.).
// NOTE: We clear KnownNetworks and KnownProxies so that the headers are accepted
// from the platform proxy. If you have a stricter network setup, restrict these.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // Accept forwarded headers from any proxy (common in PaaS). For higher security,
    // populate KnownNetworks or KnownProxies instead of clearing.
    KnownNetworks = { },
    KnownProxies = { }
});

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));
// Lightweight health endpoint for PaaS health checks
app.MapGet("/health", (ILogger<Program> logger) =>
{
    if (startupOk)
    {
        return Results.Json(new { status = "Healthy", timestamp = DateTime.UtcNow });
    }
    else
    {
        logger.LogWarning("Health check: startup tasks did not complete successfully.");
        return Results.Json(new { status = "Unhealthy", timestamp = DateTime.UtcNow, reason = "Startup tasks failed (check logs)" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

#endregion

// Run application
app.Run();

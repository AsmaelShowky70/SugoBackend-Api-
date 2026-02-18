using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SugoBackend.Data; // افترض أن هذا هو namespace الخاص بـ DbContext
using SugoBackend.Services; // افترض أن هذا هو namespace الخاص بـ TokenService
using SugoBackend.Middleware; // إضافة الـ Namespace الجديد
using Microsoft.AspNetCore.ResponseCompression; // لضغط الاستجابة
using Microsoft.AspNetCore.SignalR;
using SugoBackend.Hubs;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// --- 1. تكوين Forwarded Headers ---
// هذا هو الجزء الأهم لحل مشكلة "Invalid Hostname" في بيئة الاستضافة
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // مهم جداً لـ Railway: مسح الشبكات والبروكسيات المعروفة للوثوق بكل شيء
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// --- 2. إضافة الخدمات إلى الحاوية (Services to the container) ---

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SugoBackend API v1", Version = "v1" });
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = "X-Project-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "API key required to execute requests. Add as: X-Project-Key: {your_key}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// إضافة Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.AddSignalR();

var isDevelopment = builder.Environment.IsDevelopment();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    });
});

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IGiftService, GiftService>();
builder.Services.AddScoped<IMatchingService, MatchingService>();

// --- 3. تكوين مصادقة JWT ---
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
    throw new InvalidOperationException("JWT Key is not configured in appsettings.json");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// تكوين سياسة CORS (اختياري لكن مهم)
builder.Services.AddCors(options =>
{
    if (isDevelopment)
    {
        options.AddPolicy("AllowSpecificOrigin", policy =>
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        });
    }
    else
    {
        options.AddPolicy("AllowSpecificOrigin", policy =>
        {
            policy.WithOrigins("https://your-frontend-domain.com")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    }
});

var apiKeyEnabled = builder.Configuration.GetValue<bool>("ApiKeyProtection:Enabled");
var apiKeyHeaderName = builder.Configuration.GetValue<string>("ApiKeyProtection:HeaderName") ?? "X-Project-Key";
var apiKeyValue = builder.Configuration.GetValue<string>("ApiKeyProtection:Key");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Applying pending migrations (if any)");
        db.Database.Migrate();
        logger.LogInformation("Database migration completed successfully.");
    }
    catch (Exception ex)
    {
        // If migration fails (e.g., DB unreachable), log error but allow the app to start
        logger.LogError(ex, "Database migration failed during startup. The application will continue to run, but database operations may fail until the issue is resolved.");
    }
}

// --- 4. تكوين الـ Middleware Pipeline ---

// استخدام Forwarded Headers أولاً
app.UseForwardedHeaders();

// استخدام Middleware معالجة الأخطاء الشاملة
app.UseMiddleware<ExceptionMiddleware>();

// استخدام ضغط الاستجابة
app.UseResponseCompression();

if (apiKeyEnabled)
{
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/health", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        if (string.IsNullOrEmpty(apiKeyValue))
        {
            await next();
            return;
        }

        if (!context.Request.Headers.TryGetValue(apiKeyHeaderName, out var extractedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API key is missing.");
            return;
        }

        if (!string.Equals(extractedApiKey, apiKeyValue, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Invalid API key.");
            return;
        }

        await next();
    });
}

// تمكين Swagger دائماً (لأغراض الاختبار في الإنتاج)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SugoBackend API v1");
    c.RoutePrefix = "swagger"; // الوصول عبر /swagger
});

if (app.Environment.IsDevelopment())
{
    // app.UseSwagger();
    // app.UseSwaggerUI();
    // في بيئة التطوير المحلية قد نحتاج HTTPS
    // app.UseHttpsRedirection();
}

// في بيئة الإنتاج خلف Railway Proxy، يتم إنهاء SSL في الخارج،
// لذا لا نحتاج لإجبار HTTPS داخل الحاوية لأن الاتصال الداخلي HTTP.
// app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigin"); // استخدام سياسة CORS المحددة
app.UseAuthentication(); // المصادقة أولاً
app.UseAuthorization(); // ثم التفويض
app.MapControllers();

app.MapHub<RoomHub>("/hubs/room");

// تحديث فحص الصحة ليشمل قاعدة البيانات
app.MapGet("/health", async (AppDbContext db) =>
{
    try
    {
        if (await db.Database.CanConnectAsync())
        {
            return Results.Json(new { status = "ok", database = "connected" });
        }
        else
        {
            // محاولة فتح الاتصال يدوياً لالتقاط الخطأ الحقيقي
            // لأن CanConnectAsync يعود بـ false فقط دون تفاصيل
            try
            {
                await db.Database.OpenConnectionAsync();
                await db.Database.CloseConnectionAsync();
                return Results.Json(new { status = "ok", database = "connected_retry" });
            }
            catch (Exception ex)
            {
                return Results.Json(new { status = "error", database = "disconnected", error = ex.Message }, statusCode: 503);
            }
        }
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "error", database = "exception", error = ex.Message }, statusCode: 503);
    }
});

// مسار رئيسي ترحيبي
app.MapGet("/", () => Results.Ok(new
{
    message = "Welcome to SugoBackend API",
    status = "Running",
    docs = "/swagger",
    health = "/health"
}));

app.Run();

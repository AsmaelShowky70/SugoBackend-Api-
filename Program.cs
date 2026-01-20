using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SugoBackend.Data; // افترض أن هذا هو namespace الخاص بـ DbContext
using SugoBackend.Services; // افترض أن هذا هو namespace الخاص بـ TokenService
using Npgsql.EntityFrameworkCore.PostgreSQL;
using SugoBackend.Middleware; // إضافة الـ Namespace الجديد
using Microsoft.AspNetCore.ResponseCompression; // لضغط الاستجابة

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
builder.Services.AddSwaggerGen();

// إضافة Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var isDevelopment = builder.Environment.IsDevelopment();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    // استخدام Npgsql دائماً للاتصال بقاعدة بيانات Neon PostgreSQL
    // تم إزالة دعم SQL Server و InMemory لتوحيد بيئة العمل وضمان الاستقرار
    options.UseNpgsql(connectionString);
});

// تكوين خدمة إنشاء التوكن
builder.Services.AddScoped<ITokenService, TokenService>();

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

var app = builder.Build();

// --- 4. تكوين الـ Middleware Pipeline ---

// استخدام Forwarded Headers أولاً
app.UseForwardedHeaders();

// استخدام Middleware معالجة الأخطاء الشاملة
app.UseMiddleware<ExceptionMiddleware>();

// استخدام ضغط الاستجابة
app.UseResponseCompression();

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
            return Results.Json(new { status = "error", database = "disconnected" }, statusCode: 503);
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

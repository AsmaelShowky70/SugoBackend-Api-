═════════════════════════════════════════════════════════════════════════════════
دليل الأمان (SECURITY GUIDE)
مشروع SugoBackend — معايير الأمان والحماية
═════════════════════════════════════════════════════════════════════════════════

═════════════════════════════════════════════════════════════════════════════════
1. حماية البيانات الحساسة (Secrets Management)
═════════════════════════════════════════════════════════════════════════════════

❌ لا تفعل:
   - لا تضع JWT Secret Key أو كلمات مرور في الكود
   - لا تشارك ملف .env عبر البريد أو Git
   - لا تستخدم نفس الـ secrets في التطوير والإنتاج

✅ افعل:
   - استخدم متغيرات البيئة (Environment Variables)
   - استخدم Secrets Management Services:
     * Azure Key Vault (للـ Azure deployments)
     * AWS Secrets Manager (للـ AWS)
     * HashiCorp Vault (للـ on-prem)
     * Docker Secrets (للـ Docker Swarm)

مثال آمن:
```
// في Program.cs
var jwtKey = builder.Configuration["Jwt:Key"];
// الـ value تأتي من البيئة أو Secrets Manager، ليس من appsettings.json
```

═════════════════════════════════════════════════════════════════════════════════
2. حماية المصادقة (Authentication Security)
═════════════════════════════════════════════════════════════════════════════════

✅ JWT Token Best Practices:
   - استخدم Secret Key قوي جداً (32+ حرف عشوائي)
   - حدّد صلاحية قصيرة للـ token (30-60 دقيقة)
   - استخدم Refresh Tokens لـ tokens طويلة الأمد (اختياري)
   - تحقق من signature على كل request
   - استخدم HTTPS فقط لنقل الـ tokens

✅ كلمات المرور (Passwords):
   ❌ الطريقة الحالية (SHA256): غير آمنة كافياً
   ✅ الطريقة الصحيحة (BCrypt أو PBKDF2):
   
   اضِف package:
   ```
   dotnet add package BCrypt.Net-Next
   ```

   استخدام BCrypt:
   ```csharp
   // Hashing (في Registration)
   string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
   
   // Verification (في Login)
   bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
   ```

═════════════════════════════════════════════════════════════════════════════════
3. حماية قاعدة البيانات (Database Security)
═════════════════════════════════════════════════════════════════════════════════

✅ Connection String Best Practices:
   - لا تضع connection string في الكود
   - استخدم متغيرات البيئة
   - استخدم SQL Authentication (User ID/Password) قوية
   - فعّل Encryption على الاتصال:
     ```
     Encrypt=True;TrustServerCertificate=False
     ```

✅ حماية من SQL Injection:
   - استخدم Entity Framework Core (موجود بالفعل)
   - لا تستخدم Raw SQL إلا إذا اضطررت
   - إذا استخدمت Raw SQL استخدم Parameterized Queries

✅ Backup والنسخ الاحتياطية:
   - نسخ احتياطية يومية (automated)
   - احفظ backups في مكان آمن
   - اختبر restore processes بانتظام

═════════════════════════════════════════════════════════════════════════════════
4. HTTPS و SSL/TLS
═════════════════════════════════════════════════════════════════════════════════

✅ In Production:
   - استخدم شهادات SSL/TLS من Certificate Authority موثوق:
     * Let's Encrypt (مجاني)
     * DigiCert / Sectigo / GoDaddy
   
   - في Azure:
     ```
     App Service → TLS/SSL Settings → Add certificate
     ```
   
   - على Linux مع Let's Encrypt:
     ```
     sudo certbot certonly --standalone -d yourdomain.com
     sudo certbot renew --dry-run  # لاختبار التجديد التلقائي
     ```

✅ HTTP Strict Transport Security (HSTS):
   في Program.cs:
   ```csharp
   if (!app.Environment.IsDevelopment())
   {
       app.UseHsts();
   }
   ```

═════════════════════════════════════════════════════════════════════════════════
5. CORS والحماية من الهجمات (CORS & Cross-Site Attacks)
═════════════════════════════════════════════════════════════════════════════════

❌ الطريقة غير الآمنة (الحالية):
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()    // ❌ خطر!
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
```

✅ الطريقة الآمنة:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", builder =>
    {
        builder.WithOrigins("https://yourdomain.com", "https://app.yourdomain.com")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// استخدم الـ policy الصحيحة:
app.UseCors("Production");  // بدلاً من "AllowAll"
```

✅ حماية من CSRF (Cross-Site Request Forgery):
   - JWT Tokens توفر حماية أساسية
   - اضِف CSRF tokens للـ forms إذا كانت موجودة

═════════════════════════════════════════════════════════════════════════════════
6. Input Validation (التحقق من المدخلات)
═════════════════════════════════════════════════════════════════════════════════

✅ التحقق الحالي:
   - موجود في AuthController
   - تحقق من required fields

✅ تحسينات إضافية:
   اضِف Data Annotations:
   ```csharp
   public class RegisterDto
   {
       [Required]
       [StringLength(100, MinimumLength = 3)]
       public string Username { get; set; }
       
       [Required]
       [EmailAddress]
       public string Email { get; set; }
       
       [Required]
       [StringLength(255, MinimumLength = 8)]
       [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$")]
       public string Password { get; set; }
   }
   ```

═════════════════════════════════════════════════════════════════════════════════
7. التسجيل والمراقبة (Logging & Monitoring)
═════════════════════════════════════════════════════════════════════════════════

✅ استخدم Serilog للـ logging محسّن:
   ```
   dotnet add package Serilog.AspNetCore
   dotnet add package Serilog.Sinks.File
   ```

   في Program.cs:
   ```csharp
   var logger = new LoggerConfiguration()
       .MinimumLevel.Information()
       .WriteTo.Console()
       .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
       .CreateLogger();
   
   builder.Host.UseSerilog(logger);
   ```

✅ ماذا تسجّل:
   - Failed login attempts
   - Data modifications (create/update/delete)
   - Errors والاستثناءات
   - API usage statistics

❌ ماذا لا تسجّل:
   - كلمات المرور
   - Tokens
   - أرقام البطاقات الائتمانية

═════════════════════════════════════════════════════════════════════════════════
8. Rate Limiting (تحديد معدل الطلبات)
═════════════════════════════════════════════════════════════════════════════════

✅ اضِف Rate Limiting لـ prevent brute force attacks:
   ```
   dotnet add package AspNetCoreRateLimit
   ```

   في Program.cs:
   ```csharp
   builder.Services.AddMemoryCache();
   builder.Services.ConfigureHttpClientDefaults(http =>
   {
       http.AddStandardHttpErrorPages();
   });
   builder.Services.AddInMemoryRateLimiting();
   builder.AddRateLimitConfiguration();
   
   // في middleware
   app.UseRateLimiter();
   ```

═════════════════════════════════════════════════════════════════════════════════
9. Error Handling الآمن (Security Error Handling)
═════════════════════════════════════════════════════════════════════════════════

❌ افشِ تفاصيل الأخطاء:
   ```csharp
   // ❌ لا تفشِ معلومات التطبيق
   return BadRequest("Database error: " + ex.Message);
   ```

✅ رسائل خطأ عامة:
   ```csharp
   // ✅ رسائل آمنة
   catch (Exception ex)
   {
       logger.LogError(ex, "An error occurred");
       return StatusCode(500, new { message = "An error occurred. Please try again later." });
   }
   ```

═════════════════════════════════════════════════════════════════════════════════
10. Dependency Injection الآمن
═════════════════════════════════════════════════════════════════════════════════

✅ استخدام Scoped Services:
   ```csharp
   // ✅ صحيح
   builder.Services.AddScoped<ITokenService, TokenService>();
   builder.Services.AddScoped<AuthService>();
   ```

═════════════════════════════════════════════════════════════════════════════════
11. قائمة تدقيق الأمان (Security Checklist)
═════════════════════════════════════════════════════════════════════════════════

☐ Secrets Management:
   ☐ لا توجد secrets في الكود
   ☐ استخدام متغيرات البيئة
   ☐ تدوير الـ keys بانتظام

☐ المصادقة:
   ☐ Passwords مشفرة بـ BCrypt أو PBKDF2
   ☐ JWT Secret قوي (32+ حرف)
   ☐ Token expiry مناسب

☐ قاعدة البيانات:
   ☐ Connection string آمنة
   ☐ SQL Injection protection
   ☐ Backups منتظمة

☐ HTTPS:
   ☐ SSL/TLS فعّل
   ☐ HSTS مفعّل
   ☐ Certificates محدّثة

☐ CORS:
   ☐ محدود إلى نطاقات محددة
   ☐ ليس "AllowAll"

☐ Input Validation:
   ☐ جميع المدخلات متحقق منها
   ☐ Data Annotations موجودة

☐ Logging:
   ☐ لا تُسجّل الأسرار
   ☐ الأخطاء مسجّلة
   ☐ Access logs موجودة

☐ Rate Limiting:
   ☐ مفعّل على sensitive endpoints
   ☐ Brute force protection

☐ Error Handling:
   ☐ لا تفشِ تفاصيل الأخطاء
   ☐ رسائل آمنة فقط

═════════════════════════════════════════════════════════════════════════════════
12. التعامل مع الثغرات الأمنية (Vulnerability Response)
═════════════════════════════════════════════════════════════════════════════════

إذا اكتشفت ثغرة أمنية:

1. تقييم الخطورة
2. التصحيح فوراً
3. اختبار الإصلاح
4. النشر للإنتاج
5. إخطار المستخدمين (إذا لزم)
6. تسجيل الحادثة

═════════════════════════════════════════════════════════════════════════════════
المراجع والموارد الإضافية
═════════════════════════════════════════════════════════════════════════════════

- OWASP Top 10: https://owasp.org/www-project-top-ten/
- Microsoft Security Best Practices: https://docs.microsoft.com/security
- .NET Security Documentation: https://docs.microsoft.com/dotnet/fundamentals/code-analysis/security
- CWE (Common Weakness Enumeration): https://cwe.mitre.org/

═════════════════════════════════════════════════════════════════════════════════
تم إعداد هذا الدليل: 12 نوفمبر 2025
آخر تحديث: النسخة 1.0
═════════════════════════════════════════════════════════════════════════════════

═════════════════════════════════════════════════════════════════════════════════
دليل النشر الشامل (DEPLOYMENT GUIDE)
مشروع SugoBackend — ASP.NET Core 9.0 API
═════════════════════════════════════════════════════════════════════════════════

معلومات سريعة:
- التطبيق: RESTful API (ASP.NET Core)
- قاعدة البيانات: SQLite (محليًا) / SQL Server أو PostgreSQL (إنتاج)
- المصادقة: JWT Bearer
- الاستضافة المدعومة: Azure App Service, Linux VPS, Docker, IIS

═════════════════════════════════════════════════════════════════════════════════
الخيار 1: النشر على Azure App Service (✅ الخيار الأفضل والأسهل)
═════════════════════════════════════════════════════════════════════════════════

المميزات:
✓ سهل جداً
✓ مراقبة تلقائية وـ logging
✓ HTTPS تلقائي
✓ scaling سهل
✓ قواعد البيانات متكاملة

الخطوات:
1) في جهازك المحلي قم بـ publish:
   ```
   cd C:\Users\Asmael\Desktop\sugo\SugoBackend
   dotnet publish -c Release -o ./publish
   ```

2) في Azure، أنشئ App Service جديد:
   - الذهاب إلى Azure Portal (https://portal.azure.com)
   - Create Resource → App Service
   - Runtime stack: .NET 9 (C#)
   - اختر الخطة المناسبة (Free tier متاح للاختبار)

3) أنشئ قاعدة بيانات:
   - إذا كنت تفضل SQL Database:
     * Azure SQL Database → Create
     * Standard tier (B_Standard_B1S - أرخص)
   - أو استخدم Azure Database for PostgreSQL

4) احصل على connection string من قاعدة البيانات:
   ```
   Server=tcp:yourserver.database.windows.net,1433;
   Initial Catalog=sugo_db;
   Persist Security Info=False;
   User ID=sqladmin;
   Password=YourSecurePassword123!;
   Encrypt=True;
   Connection Timeout=30;
   ```

5) في Azure App Service، اذهب إلى Configuration (الإعدادات):
   - أضِف Application Settings (متغيرات البيئة):
     
     Key: ConnectionStrings__DefaultConnection
     Value: (paste connection string أعلاه)
     
     Key: Jwt__Key
     Value: (مفتاح قوي عشوائي: مثال: aK8#mP9@xL2$vQ1%rT4&sW5!nE6^jH7*)
     
     Key: Jwt__Issuer
     Value: SugoBackend
     
     Key: Jwt__Audience
     Value: SugoFrontend
     
     Key: ASPNETCORE_ENVIRONMENT
     Value: Production

6) نشر الملفات:
   - خيار أ: استخدام VS Code Azure Extension
     * Install: Azure Tools extension
     * Right-click on publish folder → Deploy to App Service
   
   - خيار ب: استخدام Azure CLI
     ```
     az login
     az appservice up --name SugoBackendAPI --resource-group YourResourceGroup
     ```
   
   - خيار ج: استخدم Visual Studio:
     * Right-click on project → Publish
     * Choose Azure App Service
     * اتبع المعالج

7) تطبيق الـ Migrations على قاعدة البيانات:
   ```
   cd C:\Users\Asmael\Desktop\sugo\SugoBackend
   dotnet ef database update --configuration Release --environment Production
   ```
   أو اسمح للتطبيق بتطبيق الـ migrations تلقائياً عند البداية (موجود في Program.cs)

8) اختبر الـ API:
   - افتح: https://yourappname.azurewebsites.net/swagger
   - ستجد Swagger UI وتستطيع اختبار الـ endpoints

═════════════════════════════════════════════════════════════════════════════════
الخيار 2: النشر على Linux VPS باستخدام Docker (✅ مرن وآمن)
═════════════════════════════════════════════════════════════════════════════════

المميزات:
✓ تحكم كامل
✓ رخيص نسبياً
✓ يعمل على أي VPS (DigitalOcean, Linode, AWS EC2)
✓ سهل التوسيع

الخطوات:

أ) إنشاء Dockerfile:
   اكتب ملف جديد اسمه Dockerfile في جذر المشروع:

---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SugoBackend.csproj", "."]
RUN dotnet restore "SugoBackend.csproj"
COPY . .
RUN dotnet build "SugoBackend.csproj" -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app/build .
EXPOSE 5000 5001
ENV ASPNETCORE_URLS=http://+:5000;https://+:5001
CMD ["dotnet", "SugoBackend.dll"]
---

ب) إنشاء docker-compose.yml:

---
version: '3.8'
services:
  api:
    build: .
    ports:
      - "5000:5000"
      - "5001:5001"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=db;User=sa;Password=YourPassword123!;Database=sugo_db
      - Jwt__Key=aK8#mP9@xL2$vQ1%rT4&sW5!nE6^jH7*
      - Jwt__Issuer=SugoBackend
      - Jwt__Audience=SugoFrontend
    depends_on:
      - db
  
  db:
    image: mcr.microsoft.com/mssql/server:latest
    environment:
      - SA_PASSWORD=YourPassword123!
      - ACCEPT_EULA=Y
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql

volumes:
  sqldata:
---

ج) على VPS قم بـ:
   ```
   # تثبيت Docker و Docker Compose
   sudo apt-get install docker.io docker-compose

   # انسخ ملفات المشروع
   git clone <your-repo-url>
   cd SugoBackend

   # شغّل التطبيق والـ database
   docker-compose up -d

   # اختبر
   curl http://localhost:5000/swagger
   ```

د) تهيئة HTTPS (Let's Encrypt):
   ```
   sudo apt-get install certbot python3-certbot-nginx
   sudo certbot certonly --standalone -d yourdomain.com
   ```

═════════════════════════════════════════════════════════════════════════════════
الخيار 3: النشر على Windows Server (IIS)
═════════════════════════════════════════════════════════════════════════════════

الخطوات:

1) على Windows Server:
   - ثبّت .NET 9 Runtime
   - ثبّت IIS
   - ثبّت IIS Module للـ ASP.NET Core (Hosting Bundle)

2) نشر الملفات:
   ```
   dotnet publish -c Release -o "C:\inetpub\wwwroot\sugo-api"
   ```

3) في IIS Manager:
   - Add Website
   - Path: C:\inetpub\wwwroot\sugo-api
   - Port: 80 (أو 443 مع HTTPS)
   - Application pool: No Managed Code

4) أضِف متغيرات البيئة:
   - في ملف appsettings.Production.json أو عن طريق System Environment Variables

═════════════════════════════════════════════════════════════════════════════════
الخيار 4: النشر على AWS EC2
═════════════════════════════════════════════════════════════════════════════════

الخطوات:

1) إنشاء EC2 instance:
   - اختر AMI مع .NET 9 (أو Ubuntu مع Docker)
   - اختر حجم الـ instance (t3.small يكفي للبداية)

2) في الـ instance:
   ```
   # تحديث النظام
   sudo yum update -y
   
   # تثبيت .NET
   sudo dnf install dotnet-sdk-9.0 dotnet-runtime-9.0
   
   # نسخ المشروع
   git clone <your-repo-url>
   cd SugoBackend
   
   # تشغيل
   dotnet run -c Release
   ```

3) أضِف RDS (قاعدة بيانات AWS):
   - RDS → Create database
   - اختر SQL Server أو PostgreSQL
   - ربط connection string في appsettings

4) اربط Elastic IP:
   - EC2 → Elastic IPs → Allocate → Associate مع instance

═════════════════════════════════════════════════════════════════════════════════
تغيير قاعدة البيانات من SQLite إلى SQL Server (إنتاج)
═════════════════════════════════════════════════════════════════════════════════

1) تحديث SugoBackend.csproj:
   أضِف package:
   ```xml
   <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
   ```

2) تعديل Program.cs:
   بدلاً من:
   ```csharp
   options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
   ```
   
   استخدم:
   ```csharp
   var env = builder.Environment;
   if (env.IsDevelopment())
   {
       options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
   }
   else
   {
       options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
   }
   ```

3) في appsettings.Production.json أو Environment Variables:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=your-server.database.windows.net;Database=sugo_db;User Id=sa;Password=YourPassword123!;"
     }
   }
   ```

═════════════════════════════════════════════════════════════════════════════════
قائمة تدقيق ما قبل النشر (Pre-deployment Checklist)
═════════════════════════════════════════════════════════════════════════════════

☑️ كود المشروع:
   ☐ اختبار محلي كامل للـ API
   ☐ لا توجد hardcoded secrets في الكود
   ☐ Logging موضوع في الأماكن المهمة
   ☐ Error handling شامل

☑️ الأمان:
   ☐ مفتاح JWT قوي (32+ حرف عشوائي)
   ☐ HTTPS مفعل
   ☐ CORS محدود إلى نطاقات معروفة
   ☐ AllowedHosts محدد
   ☐ كلمات المرور مشفرة بشكل قوي
   ☐ لا تُخزّن أسرار في appsettings.json

☑️ قاعدة البيانات:
   ☐ نسخة احتياطية تم أخذها
   ☐ Migrations محدثة
   ☐ قاعدة الإنتاج منفصلة عن التطوير
   ☐ Backup automated مفعل

☑️ الاستضافة:
   ☐ اختيار مزود استضافة موثوق
   ☐ Storage كافي
   ☐ Bandwidth كافي
   ☐ Support جيد

☑️ المراقبة:
   ☐ Logging و Monitoring مفعل
   ☐ Alerts مفعلة للأخطاء
   ☐ Dashboard للمراقبة

☑️ التوثيق:
   ☐ README محدث
   ☐ API documentation كاملة
   ☐ خطوات الطوارئ موثقة

═════════════════════════════════════════════════════════════════════════════════
حل المشاكل الشائعة أثناء النشر
═════════════════════════════════════════════════════════════════════════════════

مشكلة: "Connection timeout to database"
الحل:
- تأكد من connection string صحيح
- تأكد من firewall السماح بـ port 1433 (SQL Server) أو 5432 (PostgreSQL)
- اختبر الاتصال محلياً قبل النشر

مشكلة: "JWT token invalid"
الحل:
- تأكد من أن Jwt:Key متطابق في جميع البيئات
- تحقق من أن الـ token لم ينتهِ

مشكلة: "CORS error"
الحل:
- في Program.cs حدّد النطاقات المسموح بها:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", builder =>
    {
        builder.WithOrigins("https://yourdomain.com")
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
```

مشكلة: "Migrations not applied"
الحل:
```
dotnet ef database update --configuration Release
```
أو اسمح للتطبيق بتطبيقها عند البداية (موجود في Program.cs بالفعل)

═════════════════════════════════════════════════════════════════════════════════
مراقبة الـ API بعد النشر
═════════════════════════════════════════════════════════════════════════════════

1) Azure Application Insights (إذا استخدمت Azure):
   - معلومات تفصيلية عن الأداء
   - تنبيهات الأخطاء
   - Analytics

2) ELK Stack (Elasticsearch, Logstash, Kibana):
   - لتطبيقات أكبر
   - مراقبة شاملة

3) Simple Logging:
   - استخدم Serilog
   - احفظ الـ logs في ملفات أو قاعدة بيانات

═════════════════════════════════════════════════════════════════════════════════
تحديثات وصيانة بعد النشر
═════════════════════════════════════════════════════════════════════════════════

1) التحديثات الأمنية:
   - راقب .NET Security bulletins
   - حدّث الـ NuGet packages بانتظام
   ```
   dotnet list package --outdated
   dotnet package update
   ```

2) النسخ الاحتياطية:
   - قاعدة البيانات: يومياً
   - الكود: رقابة الإصدارات (Git)

3) الأداء:
   - راقب استخدام الموارد
   - حسّن الـ database queries
   - أضِف caching إذا لزم

═════════════════════════════════════════════════════════════════════════════════
الخلاصة — أيهما تختار؟
═════════════════════════════════════════════════════════════════════════════════

للمبتدئين / البداية السريعة:
→ Azure App Service (الخيار 1)
   - أسهل وأسرع
   - لا تحتاج معرفة إدارة خوادم

للمشاريع الكبيرة / التحكم الكامل:
→ Docker على Linux VPS (الخيار 2)
   - مرن جداً
   - رخيص على المدى الطويل

لو عندك Windows Server فعلاً:
→ IIS (الخيار 3)
   - تطبيق مباشر
   - لا تحتاج تقنيات إضافية

═════════════════════════════════════════════════════════════════════════════════
الملفات المطلوبة للنشر
═════════════════════════════════════════════════════════════════════════════════

✅ المشروع كاملاً (SugoBackend/)
✅ appsettings.Production.json (جديد)
✅ Dockerfile (للـ Docker deployment)
✅ docker-compose.yml (للـ Docker deployment)
✅ .env.example (مثال لمتغيرات البيئة)

═════════════════════════════════════════════════════════════════════════════════
تم إعداد هذا الدليل: 12 نوفمبر 2025
آخر تحديث: النسخة 1.0
═════════════════════════════════════════════════════════════════════════════════

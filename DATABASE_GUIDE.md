═════════════════════════════════════════════════════════════════════════════════════
دليل قاعدة البيانات (DATABASE GUIDE)
كيفية التعامل مع SQLite والنقل إلى SQL Server
═════════════════════════════════════════════════════════════════════════════════════

🗂️ ملف SQLite الحالي (sugo.db)
═════════════════════════════════════════════════════════════════════════════════════

موقع الملف: C:\Users\Asmael\Desktop\sugo\SugoBackend\sugo.db
الحجم: ~100 KB (قاعدة بيانات فارغة جاهزة للاختبار)
النوع: SQLite 3

المحتوى:
   • جدول Users (معرّف، اسم المستخدم، بريد إلكتروني، كلمة المرور)
   • جدول Rooms (معرّف، اسم الغرفة، منشئ الغرفة)
   • جدول __EFMigrationsHistory (تاريخ تطبيق الـ migrations)


🚀 استخدام SQLite محلياً (للتطوير)
═════════════════════════════════════════════════════════════════════════════════════

السيناريو: تريد تطوير الـ API محلياً بدون خادم

الخطوات:

1. تأكد من وجود sugo.db في مجلد المشروع
2. شغّل التطبيق:
   dotnet run

3. التطبيق سيتصل بـ SQLite محلياً
4. يمكنك إنشاء/حذف البيانات محلياً
5. الملف sugo.db يحفظ البيانات

المميزات:
✓ لا تحتاج إلى خادم منفصل
✓ سهل جداً للبدء السريع
✓ ملف واحد يحتوي كل شيء
✓ مناسب 100% للاختبار والتطوير

التحديات:
✗ لا يدعم concurrent users كثيرين (عملياً: حتى 5-10 users)
✗ لا يتوسع جيداً في الإنتاج
✗ الأداء تنخفض مع كمية كبيرة من البيانات
✗ لا يدعم backup أوتوماتي


🔧 نقل SQLite إلى SQL Server (للإنتاج)
═════════════════════════════════════════════════════════════════════════════════════

السيناريو: تريد نشر الـ API على الويب وتحتاج database احترافية

الخطوة 1: إضافة مصرف SQL Server

في SugoBackend.csproj أضِف:
```xml
<ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
</ItemGroup>
```

ثم:
```
dotnet restore
```

الخطوة 2: تعديل Program.cs

البحث عن:
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
```

استبدل بـ:
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    
    if (builder.Environment.IsDevelopment())
    {
        options.UseSqlite(connectionString);  // SQLite للتطوير
    }
    else
    {
        options.UseSqlServer(connectionString);  // SQL Server للإنتاج
    }
});
```

الخطوة 3: تحديث appsettings.json و appsettings.Production.json

في appsettings.Development.json (تطوير محلي):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=sugo.db"
  }
}
```

في appsettings.Production.json (الإنتاج):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:yourserver.database.windows.net,1433;Initial Catalog=sugo_db;Persist Security Info=False;User ID=sqladmin;Password=YourPassword123!;Encrypt=True;Connection Timeout=30;"
  }
}
```

أو استخدم متغيرات البيئة:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "${DB_CONNECTION_STRING}"
  }
}
```

الخطوة 4: إنشاء قاعدة SQL Server (على Azure أو Server)

على Azure:
1. اذهب إلى Azure Portal
2. Create Resource → SQL Database
3. ملأ التفاصيل:
   - Database name: sugo_db
   - Server: أنشئ جديد
   - Admin login: sqladmin
   - Password: قوي جداً (16+ حرف)
4. انتظر الإنشاء

الخطوة 5: تطبيق Migrations على SQL Server

```
dotnet ef database update --configuration Release --environment Production
```

أو يمكن للتطبيق تطبيقها تلقائياً عند البدء (موجود في Program.cs بالفعل):
```csharp
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();  // تطبيق الـ migrations تلقائياً
}
```

الخطوة 6: نقل البيانات الموجودة (اختياري)

إذا كانت لديك بيانات في SQLite وتريد نقلها:

```
dotnet ef migrations add MigrateData
# ثم أضِف script SQL يدوي للنقل
dotnet ef database update
```

أو استخدم أداة:
```
https://github.com/jitbit/DbUtils
```


📊 مقارنة بين SQLite و SQL Server
═════════════════════════════════════════════════════════════════════════════════════

المعيار         | SQLite          | SQL Server
─────────────────────────────────────────────────────
الأداء          | جيد جداً        | ممتاز
Concurrent      | 5-10 مستخدمين   | ملايين المستخدمين
Scalability     | محدود           | عالي جداً
التكلفة         | مجاني           | مدفوع (أو مجاني في Azure)
Setup           | ملف واحد        | خادم منفصل
Backup          | نسخ ملف          | بناء فيه
Redundancy      | معدوم          | عالي
للإنتاج؟       | لا              | نعم ✅
للتطوير؟        | نعم ✅          | نعم


🎯 التوصيات
═════════════════════════════════════════════════════════════════════════════════════

للتطوير المحلي:
→ استخدم SQLite (الملف sugo.db)
→ ملف واحد، سهل جداً

للاختبار:
→ استخدم SQLite أو SQL Server
→ كل حسب تفضيلك

للإنتاج (نشر على الويب):
→ استخدم SQL Server أو PostgreSQL
→ لا تستخدم SQLite على الويب

للفريق (متعدد المطورين):
→ استخدم SQL Server
→ يتيح collaborative work


🔄 خطوات النقل التدريجي
═════════════════════════════════════════════════════════════════════════════════════

الطريقة الآمنة للنقل (بدون فقدان البيانات):

المرحلة 1: الإعداد (لا تغيير في الإنتاج)
─────────────────────────────────────────
1. أضِف مصرف SQL Server إلى المشروع
2. عدّل Program.cs ليدعم كلا النوعين
3. اختبر محلياً

المرحلة 2: التجهيز (في بيئة ختبار)
─────────────────────────────────────
1. أنشئ SQL Server database في الاختبار
2. طبّق الـ migrations
3. نقل البيانات من SQLite (إذا لزم)

المرحلة 3: الانتقال (يوم النشر)
──────────────────────────────
1. خذ نسخة احتياطية من SQLite
2. اختبر الاتصال بـ SQL Server
3. شغّل التطبيق الجديد مع SQL Server
4. تتبع أي مشاكل

المرحلة 4: التحقق
──────────────────
1. كل شيء يعمل؟
2. الأداء جيد؟
3. البيانات كاملة؟

نعم → احتفظ بـ SQL Server ✅
لا → عُد إلى SQLite (في البيئة آمنة)


🛠️ أوامر مفيدة
═════════════════════════════════════════════════════════════════════════════════════

عرض database حالي:
```
dotnet ef dbcontext info
```

عرض الـ migrations المطبقة:
```
dotnet ef migrations list
```

رجوع إلى migration سابق:
```
dotnet ef database update [migration-name]
```

حذف آخر migration:
```
dotnet ef migrations remove
```

إعادة إنشاء قاعدة البيانات من الصفر:
```
dotnet ef database drop --force
dotnet ef database update
```

تنظيف الـ database القديم:
```
dotnet ef database update 0  # يحذف كل شيء
```


💾 النسخ الاحتياطية
═════════════════════════════════════════════════════════════════════════════════════

لـ SQLite:
1. انسخ ملف sugo.db
2. احفظه في مكان آمن
3. عند الحاجة: استبدل الملف الحالي

لـ SQL Server:
1. في Azure Portal أو SSMS
2. اذهب إلى Database Backups
3. فعّل Automatic Backups (يومي أو أسبوعي)
4. احفظ في Azure Storage

أوامر SQL Server:
```sql
-- Backup يدوي
BACKUP DATABASE sugo_db 
TO DISK = 'C:\Backups\sugo_db.bak'

-- Restore
RESTORE DATABASE sugo_db 
FROM DISK = 'C:\Backups\sugo_db.bak'
```


🔐 الأمان
═════════════════════════════════════════════════════════════════════════════════════

لـ SQLite:
   • الملف sugo.db لا يحتوي على أسرار
   • كل البيانات محلية وآمنة

لـ SQL Server:
   • استخدم Strong Passwords
   • فعّل Firewall rules
   • استخدم Encryption
   • اختم Database من الوصول غير المصرح

Connection String الآمن:
```
Server=tcp:server.database.windows.net,1433;
Initial Catalog=sugo_db;
User ID=sqladmin@server;
Password=YourStrongPassword123!;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```


═════════════════════════════════════════════════════════════════════════════════════

الخلاصة:
• استخدم sugo.db الحالي للتطوير محلياً
• عند الاستعداد للإنتاج، انتقل إلى SQL Server
• اتبع الخطوات بحذر بدون فقدان البيانات
• احتفظ بـ Backups دائماً

═════════════════════════════════════════════════════════════════════════════════════

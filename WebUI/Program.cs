using BusinessLayer.Security;
using DataAccesLayer.Abstract;
using WebUI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FluentValidation.AspNetCore;
using System.Reflection;
using BusinessLayer.Mapping;
using BusinessLayer.Container;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using WebUI.Data;
using WebUI.Middleware;
using WebUI.Hubs;
using Serilog;
using static WebUI.Helpers.GlobalHelper;

var builder = WebApplication.CreateBuilder(args);

// 📌 SignalR Canlı Bildirim Servisi
builder.Services.AddSignalR();

// 📌 Distributed Cache (Redis / In-Memory Fallback)
builder.Services.AddDistributedMemoryCache();

// 📌 Serilog Yapılandırması (Konsol & Günlük Dosya Loglama)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// 1. Uygulama ayarlarını al
var configuration = builder.Configuration;
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddAuthorization(AccessPolicies.Configure);
builder.Services.AddSingleton<IAuthorizationHandler, BlogOwnerAuthorizationHandler>();

// 2. Katman bağımlılıklarını çöz
builder.Services.ContainerDependencies();

// 3. DbContext'i DI sistemine ekle ve SQL Server bağlantısını ayarla
var connection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings__DefaultConnection ortam değişkenini veya User Secrets ayarını tanımlayın.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connection));


// 4. Identity sistemini `int` tipiyle yapılandır (IdentityUser<int>, IdentityRole<int>)
builder.Services.AddIdentity<AppUser, IdentityRole<int>>(options =>
{
    // Şifre kuralları
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;

    options.SignIn.RequireConfirmedAccount = false;

    // Hesap kilitleme ayarları
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromSeconds(10);
    options.Lockout.MaxFailedAccessAttempts = 1;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<AppDbContext>() // EF Core kullanımı
.AddDefaultTokenProviders(); // Şifre yenileme vb. için gerekli token sağlayıcılar

// 5. Razor runtime compilation ve FluentValidation
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation()
    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssembly(Assembly.Load("BusinessLayer")));

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// 6. Güvenlik mührü (her istek için yeniden kontrol yapılır)
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

// 7. Cookie ayarları (kimlik doğrulama için)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.LoginPath = "/dashboard/giris"; // Giriş yapılmadıysa buraya gider
    options.AccessDeniedPath = "/Error/AccessDenied"; // Yetki yoksa buraya gider
    options.SlidingExpiration = true;
    options.Cookie.Name = ".SpaMerkezi.Auth";
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

var app = builder.Build();

// 📌 Global Exception Middleware (Merkezi Hata Yakalama)
app.UseMiddleware<GlobalExceptionMiddleware>();

// Yerel geliştirmede örnek verilerden önce veritabanını hazırla.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
}

// Roles are safe to provision in all environments; demo users/content are development-only.
await DataSeeder.SeedRolesAsync(app.Services);
if (app.Environment.IsDevelopment())
    await DataSeeder.SeedAsync(app.Services);

// 9. Geliştirme ve üretim ortamlarına göre hata sayfası
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 10. HTTP ve statik dosya ayarları
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

// 11. Routing ve kimlik doğrulama
app.UseRouting();
app.UseAuthentication(); // [Authorize] için gerekli
app.UseAuthorization();

// 12. Route ayarları
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Default}/{action=Index}/{id?}");

// 404 yönlendirme
app.MapControllerRoute(
    name: "404-CatchAll",
    pattern: "{*url}",
    defaults: new { controller = "Error", action = "PageNotFound" });

// 📌 SignalR Hub Endpoint
app.MapHub<NotificationHub>("/notificationHub");

// 13. Uygulamayı çalıştır
app.Run();

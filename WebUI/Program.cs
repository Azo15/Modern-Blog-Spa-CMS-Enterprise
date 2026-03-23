using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FluentValidation.AspNetCore;
using System.Reflection;
using BusinessLayer.Mapping;
using BusinessLayer.Container;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using WebUI.Data;
using static WebUI.Helpers.GlobalHelper;

var builder = WebApplication.CreateBuilder(args);

// 1. Uygulama ayarlarını al
var configuration = builder.Configuration;

// 2. Katman bağımlılıklarını çöz
builder.Services.ContainerDependencies();

// 3. DbContext'i DI sistemine ekle ve MySQL Server bağlantısını ayarla
var connection = builder.Configuration.GetConnectionString("DefaultConnection");

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
    options.LoginPath = "/Error/AccessDenied"; // Giriş yapılmadıysa buraya gider
    options.AccessDeniedPath = "/Error/AccessDenied"; // Yetki yoksa buraya gider
    options.SlidingExpiration = true;
    options.Cookie.Name = ".SpaMerkezi.Auth";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS zorunlu
});

var app = builder.Build();

// 8. Örnek verileri yükle (tablolar boşsa)
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
app.UseHttpsRedirection();
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

// 13. Uygulamayı çalıştır
app.Run();

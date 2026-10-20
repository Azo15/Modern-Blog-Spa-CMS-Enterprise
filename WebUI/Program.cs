using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using BusinessLayer.Abstract;
using BusinessLayer.Security;
using DataAccesLayer.Abstract;
using WebUI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FluentValidation.AspNetCore;
using BusinessLayer.Container;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using WebUI.Data;
using WebUI.Middleware;
using WebUI.Hubs;
using Serilog;
using BusinessLayer;
using BusinessLayer.Jobs;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using WebUI.Models;
using WebUI.Security;
using static WebUI.Helpers.GlobalHelper;
using WebUI.Api;
using WebUI.Infrastructure;
using WebUI.Logging;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRateLimiter(options => options.AddPolicy("ai-generation", context =>
    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions { PermitLimit=3, Window=TimeSpan.FromMinutes(1), QueueLimit=0 })));




// 📌 SignalR Canlı Bildirim Servisi
builder.Services.AddSignalR();

// 📌 Distributed Cache (Redis / In-Memory Fallback)
var redis = builder.Configuration.GetConnectionString("Redis");
if (string.IsNullOrWhiteSpace(redis)) builder.Services.AddDistributedMemoryCache(options => options.SizeLimit = 50 * 1024 * 1024);
else builder.Services.AddStackExchangeRedisCache(options =>
{
    var configuration = StackExchange.Redis.ConfigurationOptions.Parse(redis);
    configuration.AbortOnConnectFail = false;
    configuration.ConnectTimeout = 1000;
    configuration.AsyncTimeout = 1000;
    configuration.SyncTimeout = 1000;
    options.ConfigurationOptions = configuration;
    options.InstanceName = "spa:";
});
builder.Services.AddScoped<IBlogImageStorage, BlogImageStorage>();
builder.Services.AddScoped<IImageOptimizationService, ImageOptimizationService>();
builder.Services.AddSingleton<PublicMediaService>();
builder.Services.AddScoped<IManagerNotifier, ManagerNotifier>();
builder.Services.AddHttpClient<IReCaptchaVerifier, ReCaptchaVerifier>(client => client.Timeout = TimeSpan.FromSeconds(10));

// 📌 Serilog Yapılandırması (Konsol & Günlük Dosya Loglama)
var logDestination = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(builder.Environment.ContentRootPath, "logs", "log-.txt"), rollingInterval: RollingInterval.Day)
    .CreateLogger();
Log.Logger = new LoggerConfiguration().MinimumLevel.Information()
    .WriteTo.Sink(new SensitiveDataRedactingSink(logDestination)).CreateLogger();

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
var testing = builder.Environment.IsEnvironment("Testing");
var localHttp = builder.Configuration.GetValue<bool>("Deployment:LocalHttp");
var connection = builder.Configuration.GetConnectionString("DefaultConnection");
if (!testing)
{
    if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Configure ConnectionStrings__DefaultConnection through secrets/environment.");
    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connection));
    builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("Hangfire") ?? connection,
        new SqlServerStorageOptions
        {
            QueuePollInterval = TimeSpan.Zero, SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            PrepareSchemaIfNecessary = builder.Environment.IsDevelopment() || args.Contains("--initialize-database", StringComparer.Ordinal)
        }));
    builder.Services.AddHangfireServer(options => options.WorkerCount = Math.Max(1, Math.Min(Environment.ProcessorCount, 4)));
}


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
.AddErrorDescriber<LocalizedIdentityErrorDescriber>()
.AddDefaultTokenProviders(); // Şifre yenileme vb. için gerekli token sağlayıcılar

// 4.1. Harici oturum açma (Google OAuth)
if (!string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Google:ClientId"]) &&
    !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Google:ClientSecret"]))
{
    builder.Services.AddAuthentication().AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.SaveTokens = false;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ClaimActions.MapJsonKey("urn:xspa:email_verified", "email_verified", System.Security.Claims.ClaimValueTypes.Boolean);
        options.Events.OnRemoteFailure = WebUI.Infrastructure.GoogleLoginFailureHandler.HandleAsync;
    });
}
builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
builder.Services.AddPlatformApi(builder.Configuration);
builder.Services.AddPlatformRateLimiting(builder.Configuration);

var keysPath = builder.Configuration["DataProtection:KeysPath"];
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("ModernBlogPlatform");
if (!string.IsNullOrWhiteSpace(keysPath)) dataProtection.PersistKeysToFileSystem(new DirectoryInfo(Path.GetFullPath(keysPath)));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    var proxies = (builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
        .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToArray();
    foreach (var proxy in proxies)
    {
        if (System.Net.IPAddress.TryParse(proxy, out var ip))
            options.KnownProxies.Add(ip);
    }
});

// 5. Razor runtime compilation ve FluentValidation
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options => options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource)))
    .AddRazorRuntimeCompilation();
builder.Services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = LanguageSettings.Supported.Select(name => new CultureInfo(name)).ToList();
    options.DefaultRequestCulture = new RequestCulture(LanguageSettings.Default);
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
    options.RequestCultureProviders = [new CookieRequestCultureProvider()];
    options.ApplyCurrentCultureToResponseHeaders = true;
});

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
    options.AccessDeniedPath = "/hata/403";
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (ApiProblemResponses.IsApi(context.HttpContext)) return ApiProblemResponses.WriteAsync(context.HttpContext, StatusCodes.Status403Forbidden);
        if (context.Request.WantsJson())
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return context.Response.WriteAsJsonAsync(new { success = false, message = "Bu işlem için erişim izniniz yok." });
        }
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToLogin = context =>
    {
        if (ApiProblemResponses.IsApi(context.HttpContext)) return ApiProblemResponses.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized);
        if (context.Request.WantsJson())
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return context.Response.WriteAsJsonAsync(new { success = false, message = "Oturumunuz sona erdi. Lütfen yeniden giriş yapın." });
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.SlidingExpiration = true;
    options.Cookie.Name = ".SpaMerkezi.Auth";
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() || localHttp
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

var app = builder.Build();
if (args.Contains("--initialize-database", StringComparer.Ordinal))
{
    await DeploymentDatabaseInitializer.InitializeAsync(app.Services, app.Configuration);
    return;
}

// Explicit opt-in for shared hosts that cannot run the deployment CLI command.
// Disable this setting after the initial schema preparation succeeds.
if (!testing && app.Configuration.GetValue<bool>("Deployment:InitializeDatabaseOnStartup"))
    await DeploymentDatabaseInitializer.InitializeAsync(app.Services, app.Configuration);

// Business exceptions are mapped below, inside the environment exception handler.

// Yerel geliştirmede örnek verilerden önce veritabanını hazırla.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
}

// Roles are safe to provision in all environments; demo users/content are development-only.
if (!testing) await DataSeeder.SeedRolesAsync(app.Services);
if (!testing)
{
    using var catalogScope = app.Services.CreateScope();
    await DataSeeder.SyncServiceCatalogAsync(catalogScope.ServiceProvider.GetRequiredService<AppDbContext>());
}
if (app.Environment.IsDevelopment())
    await DataSeeder.SeedAsync(app.Services);

if (!testing)
using (var scope = app.Services.CreateScope())
{
    var jobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    var timeZone = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "Turkey Standard Time" : "Europe/Istanbul");
    jobs.AddOrUpdate<TrashMaintenanceJob>("nightly-trash-maintenance", job => job.RunAsync(CancellationToken.None),
        Cron.Daily(2), new RecurringJobOptions { TimeZone = timeZone });
    jobs.AddOrUpdate<ContactEmailRecoveryJob>("recover-contact-emails", job => job.RecoverAsync(CancellationToken.None),
        Cron.MinuteInterval(10), new RecurringJobOptions { TimeZone = timeZone });
    jobs.AddOrUpdate<AppointmentEmailRecoveryJob>("recover-appointment-emails", job => job.RecoverAsync(CancellationToken.None),
        Cron.MinuteInterval(10), new RecurringJobOptions { TimeZone = timeZone });
}

// 9. Geliştirme ve üretim ortamlarına göre hata sayfası
app.UseForwardedHeaders();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Map validation/authorization exceptions before DeveloperExceptionPage can turn them into 500.
app.UseRequestLocalization();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<ApiBoundaryMiddleware>();
app.UsePlatformSwagger();

// 10. HTTP ve statik dosya ayarları
if (!app.Environment.IsDevelopment() && !testing && !localHttp)
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

// 11. Routing ve kimlik doğrulama
app.UseRouting();
app.UseAuthentication(); // [Authorize] için gerekli
app.UseRateLimiter();
app.UseAuthorization();
if (!testing)
{
    Hangfire.Dashboard.DashboardRoutes.AddStylesheet(typeof(Program).Assembly, "WebUI.Branding.Hangfire.css");
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new HangfireCustomAuthorizationFilter()],
        IgnoreAntiforgeryToken = false,
        AppPath = "/AdminDashboard",
        DashboardTitle = "XSPA · Arka Plan İşleri",
        FaviconPath = "/favicon.ico",
        DarkModeEnabled = false
});
}

// 12. Route ayarları
app.MapControllers();
app.Map("/api/v1/{**unmatched}", context => ApiProblemResponses.WriteAsync(context, StatusCodes.Status404NotFound));
app.MapGet("/health/ready", async (AppDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct) ? Results.Ok(new { status = "ready" }) : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));
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
public partial class Program { }

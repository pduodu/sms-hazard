using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SMSHazard.Application;
using SMSHazard.Application.Interfaces;
using SMSHazard.Infrastructure;
using SMSHazard.Infrastructure.Identity;
using SMSHazard.Infrastructure.Persistence;
using SMSHazard.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Keep local/dev runs independent of Windows Event Log permissions.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// --- Kestrel binds to loopback; Apache is the public face (see vps-deploy-plan V5/V6).
// In Production, ASPNETCORE_URLS from the systemd env file drives the bind; this keeps
// local runs consistent.
if (builder.Environment.IsProduction())
{
    builder.WebHost.UseUrls("http://127.0.0.1:5000");
}

// --- Respect the reverse proxy's forwarded scheme/host so HTTPS redirects & cookies are correct.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

// --- Persist Data-Protection keys outside the deploy dir so auth/antiforgery survive redeploys.
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
{
    Directory.CreateDirectory(keysPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
        .SetApplicationName("SMSHazard");
}

// --- Application + Infrastructure composition.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// --- ASP.NET Core Identity (roles: Reporter, SafetyOfficer, Manager, Admin).
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false; // demo simplicity (recorded as technical debt D6)
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

builder.Services.AddControllersWithViews();

// --- Rate limiting (AUTH-02): throttle credential and public-submission endpoints per client IP.
// Behind Apache, the real client IP comes from the forwarded headers processed above.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    static string ClientKey(HttpContext ctx) =>
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // Sign-in / password endpoints: 5 attempts per minute per IP.
    options.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ClientKey(ctx), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    // Anonymous public submissions: 10 per 10 minutes per IP.
    options.AddPolicy("public-submit", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ClientKey(ctx), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0
        }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        context.HttpContext.Response.ContentType = "text/plain";
        await context.HttpContext.Response.WriteAsync(
            "Too many attempts. Please wait a minute and try again.", token);
    };
});

// --- Hangfire: durable, PostgreSQL-backed recurring reminders + background email.
var hangfireConn = builder.Configuration.GetConnectionString("Default");
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(hangfireConn)));
builder.Services.AddHangfireServer();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Hangfire dashboard, gated to Admins (after auth so User is populated).
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AdminOnlyDashboardFilter() }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// --- Health endpoint that proves DB connectivity (used by the deploy verification).
app.MapGet("/health", async (AppDbContext db) =>
    await db.Database.CanConnectAsync() ? Results.Ok("healthy") : Results.StatusCode(503));

// --- Apply migrations on startup so schema is created/updated on each deploy.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

// Register the hourly reminder job (idempotent — safe on every startup).
RecurringJob.AddOrUpdate<IReminderService>(
    "capa-reminders",
    svc => svc.ProcessDueRemindersAsync(),
    Cron.Hourly());

// Register the monthly safety-digest job (ADM-02): 1st of each month at 07:00 UTC.
RecurringJob.AddOrUpdate<IDigestService>(
    "monthly-digest",
    svc => svc.SendMonthlyDigestAsync(),
    "0 7 1 * *");

app.Run();

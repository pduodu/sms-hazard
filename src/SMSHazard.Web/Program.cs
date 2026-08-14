using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SMSHazard.Application;
using SMSHazard.Infrastructure;
using SMSHazard.Infrastructure.Identity;
using SMSHazard.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

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
});

builder.Services.AddControllersWithViews();

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

app.Run();

using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MudBlazor.Services;
using SmartPricing.Application.DTOs;
using SmartPricing.Application.Interfaces;
using SmartPricing.Infrastructure.Data;
using SmartPricing.Infrastructure.Services;
using SmartPricing.Web.Components;
using SmartPricing.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Data Protection ───────────────────────────────────────────────────────────
// Persisted outside the publish folder so auth cookies and the stored DB password
// survive restarts and redeploys. Keys are encrypted with DPAPI on Windows.
var keysRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
if (string.IsNullOrEmpty(keysRoot)) keysRoot = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
var dataProtection = builder.Services.AddDataProtection()
    .SetApplicationName("SmartPricing")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(keysRoot, "SmartPricing", "keys")));
if (OperatingSystem.IsWindows()) dataProtection.ProtectKeysWithDpapi();

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<ConnectionStringResolver>();
builder.Services.AddDbContext<SmartPricingDbContext>((sp, options) =>
    options.UseSqlServer(sp.GetRequiredService<ConnectionStringResolver>().Resolve()));

// ── Authentication ────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdmin", p => p.RequireRole("Admin"))
    .AddPolicy("RequireApprover", p => p.RequireRole("Approver", "Admin"))
    .AddPolicy("RequireReviewer", p => p.RequireRole("Reviewer", "Approver", "Admin"))
    .AddPolicy("RequireRequester", p => p.RequireRole("Requester", "Reviewer", "Approver", "Admin"));

builder.Services.AddCascadingAuthenticationState();

// ── Login rate limiting (per client IP) ───────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
    options.OnRejected = (ctx, _) =>
    {
        ctx.HttpContext.Response.Redirect("/login?error=" +
            Uri.EscapeDataString("Πολλές προσπάθειες. Δοκιμάστε ξανά σε λίγο. / Too many attempts, try again shortly."));
        return ValueTask.CompletedTask;
    };
});

// ── MudBlazor ─────────────────────────────────────────────────────────────────
builder.Services.AddMudServices();

// ── App Services ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<AppStateService>();
builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<AppStateService>());
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IMasterDataService, MasterDataService>();
builder.Services.AddScoped<ISupplierPriceService, SupplierPriceService>();
builder.Services.AddScoped<ISellingPriceService, SellingPriceService>();
builder.Services.AddScoped<IBatchService, BatchService>();
builder.Services.AddScoped<IPricingRuleService, PricingRuleService>();
builder.Services.AddScoped<IErpIntegrationService, ErpIntegrationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<DatabaseSettingsService>();

// ── Blazor ────────────────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseRateLimiter();

// Users flagged to change their password (e.g. the seeded admin) can't use the app until they do.
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path;
    if (ctx.User.HasClaim(MustChangePasswordClaim, "true") &&
        HttpMethods.IsGet(ctx.Request.Method) &&
        !path.StartsWithSegments("/account") && !path.StartsWithSegments("/logout") &&
        !path.StartsWithSegments("/_framework") && !path.StartsWithSegments("/_content") &&
        !path.StartsWithSegments("/css"))
    {
        ctx.Response.Redirect("/account/change-password");
        return;
    }
    await next();
});

// ── DB Init on startup ───────────────────────────────────────────────────────
// Applies EF Core migrations. Databases created by older versions with EnsureCreated()
// have the full initial schema but no migration history, so they are baselined first.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SmartPricingDbContext>();
    if (await db.Database.CanConnectAsync())
    {
        var applied = await db.Database.GetAppliedMigrationsAsync();
        var initial = db.Database.GetMigrations().First();
        var usersTableExists = await db.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users' AND TABLE_SCHEMA = 'dbo'")
            .SingleAsync() > 0;

        if (!applied.Any() && usersTableExists)
        {
            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
                CREATE TABLE [__EFMigrationsHistory] (
                    [MigrationId] nvarchar(150) NOT NULL,
                    [ProductVersion] nvarchar(32) NOT NULL,
                    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId]));
                """);
            await db.Database.ExecuteSqlAsync(
                $"INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ({initial}, {ProductInfo.GetVersion()})");
        }
    }
    await db.Database.MigrateAsync();
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ── Auth endpoints ────────────────────────────────────────────────────────────
app.MapGet("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

// ── Login POST endpoint ───────────────────────────────────────────────────────
app.MapPost("/account/login", async (
    HttpContext ctx,
    IUserService userSvc,
    [FromForm] string? username,
    [FromForm] string? password,
    [FromForm] string? returnUrl) =>
{
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        return LoginError("Εισάγετε username και password.", returnUrl);

    var result = await userSvc.AuthenticateAsync(new LoginDto(username, password));
    if (result.IsLockedOut)
        return LoginError("Ο λογαριασμός κλειδώθηκε προσωρινά. Δοκιμάστε σε 15 λεπτά. / Account temporarily locked.", returnUrl);
    if (result.User is null)
        return LoginError("Λάθος username ή password.", returnUrl);

    await SignInAsync(ctx, result.User, result.MustChangePassword);

    return result.MustChangePassword
        ? Results.Redirect("/account/change-password")
        : Results.LocalRedirect(SafeLocalUrl(returnUrl));
}).RequireRateLimiting("login");

// ── Change password POST endpoint ─────────────────────────────────────────────
app.MapPost("/account/change-password", async (
    HttpContext ctx,
    IUserService userSvc,
    [FromForm] string? currentPassword,
    [FromForm] string? newPassword,
    [FromForm] string? confirmPassword) =>
{
    static IResult Error(string msg) =>
        Results.Redirect("/account/change-password?error=" + Uri.EscapeDataString(msg));

    if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
        return Error("Συμπληρώστε όλα τα πεδία. / Fill in all fields.");
    if (newPassword != confirmPassword)
        return Error("Οι κωδικοί δεν ταιριάζουν. / Passwords do not match.");
    if (PasswordPolicy.Validate(newPassword) is { } policyError)
        return Error(policyError);
    if (newPassword == currentPassword)
        return Error("Ο νέος κωδικός πρέπει να διαφέρει. / New password must differ from the current one.");

    if (!int.TryParse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userID) ||
        !await userSvc.ChangePasswordAsync(userID, currentPassword, newPassword))
        return Error("Λάθος τρέχων κωδικός. / Current password is incorrect.");

    var user = await userSvc.GetByIdAsync(userID);
    if (user is not null) await SignInAsync(ctx, user, mustChangePassword: false);
    return Results.Redirect("/");
}).RequireAuthorization().RequireRateLimiting("login");

app.Run();

static IResult LoginError(string message, string? returnUrl) =>
    Results.Redirect("/login?error=" + Uri.EscapeDataString(message) +
        (string.IsNullOrWhiteSpace(returnUrl) ? "" : "&returnUrl=" + Uri.EscapeDataString(returnUrl)));

// Only allow app-relative redirects ("/path"), never "//host" or "/\host".
static string SafeLocalUrl(string? url)
{
    if (string.IsNullOrWhiteSpace(url)) return "/";
    url = Uri.UnescapeDataString(url);
    if (!url.StartsWith('/')) url = "/" + url;
    return url.Length > 1 && (url[1] == '/' || url[1] == '\\') ? "/" : url;
}

static async Task SignInAsync(HttpContext ctx, UserDto user, bool mustChangePassword)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
        new(ClaimTypes.Name, user.UserName),
        new("DisplayName", user.DisplayName)
    };
    if (user.Email is not null) claims.Add(new(ClaimTypes.Email, user.Email));
    if (mustChangePassword) claims.Add(new(MustChangePasswordClaim, "true"));
    claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
}

public partial class Program
{
    private const string MustChangePasswordClaim = "MustChangePassword";
}

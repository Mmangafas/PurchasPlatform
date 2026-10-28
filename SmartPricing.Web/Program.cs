using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using SmartPricing.Application.Interfaces;
using SmartPricing.Infrastructure.Data;
using SmartPricing.Infrastructure.Services;
using SmartPricing.Web.Components;
using SmartPricing.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<SmartPricingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ── Authentication ────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdmin", p => p.RequireRole("Admin"))
    .AddPolicy("RequireApprover", p => p.RequireRole("Approver", "Admin"))
    .AddPolicy("RequireReviewer", p => p.RequireRole("Reviewer", "Approver", "Admin"))
    .AddPolicy("RequireRequester", p => p.RequireRole("Requester", "Reviewer", "Approver", "Admin"));

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

// ── MudBlazor ─────────────────────────────────────────────────────────────────
builder.Services.AddMudServices();

// ── App Services ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IMasterDataService, MasterDataService>();
builder.Services.AddScoped<ISupplierPriceService, SupplierPriceService>();
builder.Services.AddScoped<ISellingPriceService, SellingPriceService>();
builder.Services.AddScoped<IBatchService, BatchService>();
builder.Services.AddScoped<IPricingRuleService, PricingRuleService>();
builder.Services.AddScoped<IErpIntegrationService, ErpIntegrationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<AppStateService>();
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
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

// ── DB Init on startup ───────────────────────────────────────────────────────
// Checks if the Users table exists. If not, wipes and recreates the full schema.
// This handles the case where MigrateAsync() previously created an empty database.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SmartPricingDbContext>();
    var conn = db.Database.GetDbConnection();
    await conn.OpenAsync();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users' AND TABLE_SCHEMA = 'dbo'";
    var tableExists = (int)(await cmd.ExecuteScalarAsync() ?? 0) > 0;
    await conn.CloseAsync();

    if (!tableExists)
    {
        // DB exists but tables were never created — delete and recreate cleanly
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }
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
    AppStateService appState,
    [Microsoft.AspNetCore.Mvc.FromForm] string username,
    [Microsoft.AspNetCore.Mvc.FromForm] string password,
    [Microsoft.AspNetCore.Mvc.FromForm] string? returnUrl) =>
{
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        return Results.Redirect("/login?error=" + Uri.EscapeDataString("Εισάγετε username και password."));

    var user = await userSvc.AuthenticateAsync(new SmartPricing.Application.DTOs.LoginDto(username, password));
    if (user is null)
        return Results.Redirect("/login?error=" + Uri.EscapeDataString("Λάθος username ή password."));

    appState.SetUser(user);

    var claims = new System.Collections.Generic.List<System.Security.Claims.Claim>
    {
        new(System.Security.Claims.ClaimTypes.NameIdentifier, user.UserID.ToString()),
        new(System.Security.Claims.ClaimTypes.Name, user.UserName),
        new("DisplayName", user.DisplayName)
    };
    claims.AddRange(user.Roles.Select(r =>
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, r)));

    var identity = new System.Security.Claims.ClaimsIdentity(
        claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
        new System.Security.Claims.ClaimsPrincipal(identity));

    var redirect = string.IsNullOrWhiteSpace(returnUrl) ? "/" : Uri.UnescapeDataString(returnUrl);
    return Results.Redirect(redirect);
}).DisableAntiforgery();

app.Run();

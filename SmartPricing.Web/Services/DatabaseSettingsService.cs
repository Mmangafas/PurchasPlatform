using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using SmartPricing.Application.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SmartPricing.Web.Services;

/// <summary>
/// Builds the effective connection string. A SQL password saved from the Settings page is kept
/// encrypted (ASP.NET Data Protection) in "ConnectionStrings:DefaultPassword", never in plain text.
/// </summary>
public class ConnectionStringResolver
{
    public const string ProtectedPasswordKey = "DefaultPassword";

    private readonly IConfiguration _configuration;
    private readonly IDataProtector _protector;

    public ConnectionStringResolver(IConfiguration configuration, IDataProtectionProvider dataProtection)
    {
        _configuration = configuration;
        _protector = dataProtection.CreateProtector("SmartPricing.ConnectionString");
    }

    public string Protect(string password) => _protector.Protect(password);

    /// <summary>The connection string as written in configuration, without the protected password.</summary>
    public string Raw => _configuration.GetConnectionString("Default") ?? "";

    public string? StoredPassword
    {
        get
        {
            var encrypted = _configuration.GetConnectionString(ProtectedPasswordKey);
            return string.IsNullOrEmpty(encrypted) ? null : _protector.Unprotect(encrypted);
        }
    }

    public string Resolve()
    {
        var password = StoredPassword;
        if (password is null) return Raw;
        return new SqlConnectionStringBuilder(Raw) { Password = password }.ConnectionString;
    }
}

public class DatabaseSettingsService
{
    private readonly ConnectionStringResolver _resolver;
    private readonly ICurrentUser _user;
    private readonly string _appSettingsPath;

    public DatabaseSettingsService(ConnectionStringResolver resolver, ICurrentUser user, IWebHostEnvironment env)
    {
        _resolver = resolver;
        _user = user;
        _appSettingsPath = Path.Combine(env.ContentRootPath, "appsettings.json");
    }

    // ── Read current settings ─────────────────────────────────────────────────
    // The password is never sent to the browser; an empty password means "keep the stored one".

    public DbConnectionSettings GetCurrentSettings()
    {
        var s = ParseConnectionString(_resolver.Raw);
        s.Password = "";
        return s;
    }

    // ── Build connection string from settings ─────────────────────────────────

    public string BuildConnectionString(DbConnectionSettings s, bool includePassword = true)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource         = s.Server,
            InitialCatalog     = s.Database,
            MultipleActiveResultSets = true,
            TrustServerCertificate   = s.TrustServerCertificate,
            Encrypt            = s.Encrypt
        };

        if (s.UseWindowsAuth)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = s.Username;
            if (includePassword) builder.Password = EffectivePassword(s);
        }

        return builder.ConnectionString;
    }

    /// <summary>Connection string for display, with the password masked.</summary>
    public string BuildPreview(DbConnectionSettings s)
    {
        var cs = BuildConnectionString(s, includePassword: false);
        return s.UseWindowsAuth ? cs : cs + ";Password=********";
    }

    // ── Test connection ───────────────────────────────────────────────────────

    public async Task<(bool success, string message)> TestConnectionAsync(DbConnectionSettings s)
    {
        _user.RequireAdmin();
        var cs = BuildConnectionString(s);
        try
        {
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();
            return (true, $"✅ Connected to '{s.Server}' — database '{s.Database}'");
        }
        catch (Exception ex)
        {
            return (false, $"❌ {ex.Message}");
        }
    }

    // ── Save to appsettings.json ──────────────────────────────────────────────

    public async Task SaveConnectionStringAsync(DbConnectionSettings s)
    {
        _user.RequireAdmin();
        var password = s.UseWindowsAuth ? null : EffectivePassword(s);

        JsonNode root;
        if (File.Exists(_appSettingsPath))
        {
            var json = await File.ReadAllTextAsync(_appSettingsPath);
            root = JsonNode.Parse(json) ?? new JsonObject();
        }
        else
        {
            root = new JsonObject();
        }

        // Ensure ConnectionStrings node exists
        if (root["ConnectionStrings"] is not JsonObject connStrings)
        {
            connStrings = new JsonObject();
            root["ConnectionStrings"] = connStrings;
        }

        connStrings["Default"] = BuildConnectionString(s, includePassword: false);
        if (string.IsNullOrEmpty(password))
            connStrings.Remove(ConnectionStringResolver.ProtectedPasswordKey);
        else
            connStrings[ConnectionStringResolver.ProtectedPasswordKey] = _resolver.Protect(password);

        // Remove the hint key if present (it causes JSON parse errors)
        connStrings.Remove("_HowToUseFullSqlServer");

        var options = new JsonSerializerOptions { WriteIndented = true };
        var output  = root.ToJsonString(options);
        await File.WriteAllTextAsync(_appSettingsPath, output);
    }

    private string EffectivePassword(DbConnectionSettings s)
    {
        if (!string.IsNullOrEmpty(s.Password)) return s.Password;
        // Keep the current password (protected, or legacy plain text in the connection string).
        return _resolver.StoredPassword ?? ParseConnectionString(_resolver.Raw).Password;
    }

    // ── Parse existing connection string into settings object ─────────────────

    private static DbConnectionSettings ParseConnectionString(string cs)
    {
        var s = new DbConnectionSettings();
        if (string.IsNullOrWhiteSpace(cs)) return s;

        try
        {
            var builder = new SqlConnectionStringBuilder(cs);
            s.Server               = builder.DataSource;
            s.Database             = builder.InitialCatalog;
            s.UseWindowsAuth       = builder.IntegratedSecurity;
            s.Username             = builder.UserID;
            s.Password             = builder.Password;
            s.TrustServerCertificate = builder.TrustServerCertificate;
            s.Encrypt              = builder.Encrypt;
        }
        catch { /* return defaults on malformed string */ }

        return s;
    }
}

internal static class CurrentUserExtensions
{
    public static void RequireAdmin(this ICurrentUser user)
    {
        if (!user.IsInRole("Admin"))
            throw new UnauthorizedAccessException("Admin only.");
    }
}

// ── Settings model ────────────────────────────────────────────────────────────

public class DbConnectionSettings
{
    public string Server                { get; set; } = "(localdb)\\mssqllocaldb";
    public string Database              { get; set; } = "SmartPricingDb";
    public bool   UseWindowsAuth        { get; set; } = true;
    public string Username              { get; set; } = "";
    public string Password              { get; set; } = "";
    public bool   TrustServerCertificate { get; set; } = true;
    public bool   Encrypt               { get; set; } = false;
}

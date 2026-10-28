using Microsoft.Data.SqlClient;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SmartPricing.Web.Services;

public class DatabaseSettingsService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly string _appSettingsPath;

    public DatabaseSettingsService(IConfiguration configuration, IWebHostEnvironment env)
    {
        _configuration = configuration;
        _env = env;
        _appSettingsPath = Path.Combine(env.ContentRootPath, "appsettings.json");
    }

    // ── Read current settings ─────────────────────────────────────────────────

    public DbConnectionSettings GetCurrentSettings()
    {
        var cs = _configuration.GetConnectionString("Default") ?? "";
        return ParseConnectionString(cs);
    }

    // ── Build connection string from settings ─────────────────────────────────

    public string BuildConnectionString(DbConnectionSettings s)
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
            builder.UserID   = s.Username;
            builder.Password = s.Password;
        }

        return builder.ConnectionString;
    }

    // ── Test connection ───────────────────────────────────────────────────────

    public async Task<(bool success, string message)> TestConnectionAsync(DbConnectionSettings s)
    {
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
        var cs = BuildConnectionString(s);

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

        connStrings["Default"] = cs;

        // Remove the hint key if present (it causes JSON parse errors)
        connStrings.Remove("_HowToUseFullSqlServer");

        var options = new JsonSerializerOptions { WriteIndented = true };
        var output  = root.ToJsonString(options);
        await File.WriteAllTextAsync(_appSettingsPath, output);
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

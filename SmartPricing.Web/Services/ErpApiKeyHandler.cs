using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SmartPricing.Application.Interfaces;

namespace SmartPricing.Web.Services;

/// <summary>
/// Authenticates the ERP's calls to /api/erp/* with <c>Authorization: Bearer {Erp:ApiKey}</c>.
/// The key comes from configuration (user-secrets or the Erp__ApiKey environment variable);
/// while it is not set, the API rejects every call.
/// </summary>
public class ErpApiKeyHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, IConfiguration config)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ErpApiKey";
    private const int MinKeyLength = 32;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var expected = config["Erp:ApiKey"];
        if (string.IsNullOrEmpty(expected) || expected.Length < MinKeyLength)
            return Task.FromResult(AuthenticateResult.NoResult());

        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        // Compare hashes so the comparison is constant-time regardless of length.
        var given = SHA256.HashData(Encoding.UTF8.GetBytes(header["Bearer ".Length..].Trim()));
        if (!CryptographicOperations.FixedTimeEquals(given, SHA256.HashData(Encoding.UTF8.GetBytes(expected))))
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "ERP"), new Claim(ClaimTypes.Role, SystemRoles.ErpIntegration)], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}

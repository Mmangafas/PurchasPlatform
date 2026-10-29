using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using SmartPricing.Application.DTOs;
using SmartPricing.Application.Interfaces;

namespace SmartPricing.Web.Services;

/// <summary>
/// Scoped service exposing the current authenticated user.
/// The user is read from the auth cookie's claims, so it is available in
/// every Blazor circuit and survives reconnects and page reloads.
/// </summary>
public class AppStateService : ICurrentUser, IDisposable
{
    private readonly AuthenticationStateProvider _authProvider;
    private readonly IHttpContextAccessor _httpContext;
    private ClaimsPrincipal? _principal;
    private UserDto? _user;

    public AppStateService(AuthenticationStateProvider authProvider, IHttpContextAccessor httpContext)
    {
        _authProvider = authProvider;
        _httpContext = httpContext;
        _authProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    public UserDto? CurrentUser
    {
        get
        {
            var principal = GetPrincipal();
            if (principal is null || principal.Identity?.IsAuthenticated != true) return null;
            return _user ??= FromClaims(principal);
        }
    }

    public int CurrentUserID => CurrentUser?.UserID ?? 0;
    public bool IsAuthenticated => CurrentUser is not null;

    int ICurrentUser.UserID => CurrentUserID;

    public bool IsInRole(string role) =>
        CurrentUser?.Roles.Contains(role, StringComparer.OrdinalIgnoreCase) ?? false;

    public bool IsAdmin => IsInRole("Admin");
    public bool IsApprover => IsInRole("Approver") || IsAdmin;
    public bool IsReviewer => IsInRole("Reviewer") || IsApprover;

    public event Action? OnChange;

    public void Clear()
    {
        _principal = new ClaimsPrincipal(new ClaimsIdentity());
        _user = null;
        OnChange?.Invoke();
    }

    private ClaimsPrincipal? GetPrincipal()
    {
        if (_principal is not null) return _principal;
        try
        {
            // The server provider's state is already resolved for both HTTP requests and circuits.
            var task = _authProvider.GetAuthenticationStateAsync();
            if (task.IsCompletedSuccessfully) _principal = task.Result.User;
        }
        catch (InvalidOperationException)
        {
            // No Blazor authentication state in this scope (a minimal API endpoint): use the request's user.
            _principal = _httpContext.HttpContext?.User;
        }
        return _principal;
    }

    private async void OnAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        var state = await task;
        _principal = state.User;
        _user = null;
        OnChange?.Invoke();
    }

    private static UserDto? FromClaims(ClaimsPrincipal p)
    {
        // Service identities (the ERP API key) have no user row; they get UserID 0.
        if (!int.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) id = 0;
        var name = p.FindFirstValue(ClaimTypes.Name) ?? "";
        return new UserDto(id, name,
            p.FindFirstValue("DisplayName") ?? name,
            p.FindFirstValue(ClaimTypes.Email),
            p.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList());
    }

    public void Dispose() => _authProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
}

using SmartPricing.Application.DTOs;

namespace SmartPricing.Web.Services;

/// <summary>
/// Scoped service that holds the current authenticated user
/// throughout a Blazor Server circuit.
/// </summary>
public class AppStateService
{
    public UserDto? CurrentUser { get; private set; }
    public int CurrentUserID => CurrentUser?.UserID ?? 0;
    public bool IsAuthenticated => CurrentUser is not null;

    public bool IsInRole(string role) =>
        CurrentUser?.Roles.Contains(role, StringComparer.OrdinalIgnoreCase) ?? false;

    public bool IsAdmin => IsInRole("Admin");
    public bool IsApprover => IsInRole("Approver") || IsAdmin;
    public bool IsReviewer => IsInRole("Reviewer") || IsApprover;

    public event Action? OnChange;

    public void SetUser(UserDto? user)
    {
        CurrentUser = user;
        OnChange?.Invoke();
    }

    public void Clear()
    {
        CurrentUser = null;
        OnChange?.Invoke();
    }
}

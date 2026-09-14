namespace Centraly.Desktop.Services;

// Single-process, single-user session state - the Desktop equivalent of reading claims off
// a JWT. Set once at login by LoginViewModel, read everywhere else (permission gating,
// passing userId into service calls the same way the old controllers did via User.GetUserId()).
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string? UserId { get; }
    string? UserName { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<string> Permissions { get; }

    bool HasPermission(string permission);
    bool HasAnyRole(params string[] roleNames);

    void SignIn(LocalLoginResult login);
    void SignOut();
}

public class CurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; private set; }
    public string? UserId { get; private set; }
    public string? UserName { get; private set; }
    public IReadOnlyList<string> Roles { get; private set; } = [];
    public IReadOnlyList<string> Permissions { get; private set; } = [];

    public bool HasPermission(string permission) =>
        IsAuthenticated && (Roles.Contains("Admin") || Permissions.Contains(permission));

    public bool HasAnyRole(params string[] roleNames) =>
        IsAuthenticated && roleNames.Any(Roles.Contains);

    public void SignIn(LocalLoginResult login)
    {
        UserId = login.UserId;
        UserName = login.UserName;
        Roles = login.Roles;
        Permissions = login.Permissions;
        IsAuthenticated = true;
    }

    public void SignOut()
    {
        UserId = null;
        UserName = null;
        Roles = [];
        Permissions = [];
        IsAuthenticated = false;
    }
}

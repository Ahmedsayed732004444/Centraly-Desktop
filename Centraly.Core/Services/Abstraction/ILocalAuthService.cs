namespace Centraly.Api.Services.Abstraction;

// Desktop's sign-in path: no JWT, no HttpContext (SignInManager.PasswordSignInAsync needs
// one). Checks the password directly via UserManager and returns the session data the
// caller (Desktop's ICurrentUser) needs to hold for the rest of the process lifetime.
public interface ILocalAuthService
{
    Task<Result<LocalLoginResult>> LoginAsync(string userName, string password, CancellationToken ct = default);
}

public record LocalLoginResult(
    string UserId,
    string UserName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

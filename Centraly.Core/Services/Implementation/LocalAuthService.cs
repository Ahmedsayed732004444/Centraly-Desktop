namespace Centraly.Api.Services.Implementation;

public class LocalAuthService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext context) : ILocalAuthService
{
    public async Task<Result<LocalLoginResult>> LoginAsync(string userName, string password, CancellationToken ct = default)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.UserName == userName, ct);
        if (user is null)
            return Result.Failure<LocalLoginResult>(UserErrors.InvalidCredentials);

        if (await userManager.IsLockedOutAsync(user))
            return Result.Failure<LocalLoginResult>(UserErrors.LockedUser);

        var passwordValid = await userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            await userManager.AccessFailedAsync(user);
            return Result.Failure<LocalLoginResult>(UserErrors.InvalidCredentials);
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var permissions = await (
            from r in context.Roles
            join p in context.RoleClaims on r.Id equals p.RoleId
            where roles.Contains(r.Name!)
            select p.ClaimValue!)
            .Distinct()
            .ToListAsync(ct);

        return Result.Success(new LocalLoginResult(user.Id, user.UserName ?? string.Empty, roles.ToList(), permissions));
    }
}

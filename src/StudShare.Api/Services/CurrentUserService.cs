using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StudShare.Api.Data;
using StudShare.Api.Entities;

namespace StudShare.Api.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor, AppDbContext db)
{
    public Guid UserId => Guid.Parse(
        httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public UserRole UserRole => Enum.Parse<UserRole>(
        httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.Role)!);

    public async Task<User> GetUserAsync() =>
        await db.Users.FindAsync(UserId) ?? throw new KeyNotFoundException("User not found.");

    public async Task<AuthorSummary> GetActorAsync()
    {
        var user = await GetUserAsync();
        return new AuthorSummary
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            MiddleName = user.MiddleName,
            Role = user.Role,
        };
    }
}

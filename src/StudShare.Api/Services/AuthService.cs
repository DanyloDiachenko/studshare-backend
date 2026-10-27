using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StudShare.Api.Data;
using StudShare.Api.DTos;
using StudShare.Api.Entities;

namespace StudShare.Api.Services;

public class AuthService(AppDbContext db, IConfiguration config, HistoryService historyService)
{
    public async Task<(AuthResponse, UserResponse)> RegisterAsync(SignUpRequest req)
    {
        if (await db.Users.AnyAsync(u => u.Email == req.Email))
            throw new InvalidOperationException("Email already taken.");

        var user = new User
        {
            Email = req.Email.ToLowerInvariant(),
            FirstName = req.FirstName,
            LastName = req.LastName,
            MiddleName = req.MiddleName,
            Role = UserRole.Student,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            JoinedAt = DateTime.UtcNow,
            UploadAccess = false,
        };

        var invite = await db.GroupInvitations
            .FirstOrDefaultAsync(i => i.Email == req.Email && i.ExpiresAt > DateTime.UtcNow);

        if (invite is not null)
        {
            user.GroupId = invite.GroupId;
            user.InvitedAt = invite.InvitedAt;
            db.GroupInvitations.Remove(invite);

            await db.SaveChangesAsync();

            var actor = new AuthorSummary
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                MiddleName = user.MiddleName,
                Role = user.Role,
            };
            await historyService.LogAsync(invite.GroupId, HistoryEventKind.MemberAdded, actor,
                $"Приєднався(лась) до групи");
        }
        else
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        return (new AuthResponse(GenerateToken(user)), MapUser(user));
    }

    public async Task<(AuthResponse, UserResponse)> LoginAsync(SignInRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLowerInvariant())
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        return (new AuthResponse(GenerateToken(user)), MapUser(user));
    }

    public async Task<UserResponse> GetMeAsync(Guid userId)
    {
        var user = await db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");
        return MapUser(user);
    }

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET")!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static UserResponse MapUser(User u) => new(
        u.Id.ToString(), u.Email, u.FirstName, u.LastName, u.MiddleName,
        u.Role.ToString(), u.GroupId?.ToString(), u.UploadAccess, u.JoinedAt, u.InvitedAt,
        u.IsEmailVerified);
}

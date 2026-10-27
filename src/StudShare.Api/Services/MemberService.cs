using Microsoft.EntityFrameworkCore;
using StudShare.Api.Data;
using StudShare.Api.DTos;
using StudShare.Api.Entities;

namespace StudShare.Api.Services;

public class MemberService(AppDbContext db, HistoryService historyService)
{
    public async Task<(List<UserResponse> members, List<GroupInvitationResponse> invitations)> GetMembersAsync(Guid groupId)
    {
        var members = await db.Users
            .Where(u => u.GroupId == groupId)
            .Select(u => AuthService.MapUser(u))
            .ToListAsync();

        var invitations = await db.GroupInvitations
            .Where(i => i.GroupId == groupId)
            .Select(i => new GroupInvitationResponse(
                i.Id.ToString(), i.GroupId.ToString(), i.Email,
                new AuthorSummaryResponse(i.InvitedBy.Id.ToString(), i.InvitedBy.FirstName,
                    i.InvitedBy.LastName, i.InvitedBy.MiddleName, i.InvitedBy.Role.ToString()),
                i.InvitedAt, i.ExpiresAt))
            .ToListAsync();

        return (members, invitations);
    }

    public async Task<UserResponse> SetUploadAccessAsync(Guid groupId, Guid userId, SetUploadAccessRequest req, AuthorSummary actor)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.GroupId == groupId)
            ?? throw new KeyNotFoundException("Member not found.");

        if (user.Role != UserRole.Student)
            throw new InvalidOperationException("UploadAccess can only be changed for Students.");

        user.UploadAccess = req.UploadAccess;
        await db.SaveChangesAsync();

        var action = req.UploadAccess ? "увімкнув(ла)" : "вимкнув(ла)";
        await historyService.LogAsync(groupId, HistoryEventKind.UploadAccess, actor,
            $"{action} завантаження для {user.FirstName} {user.LastName}");

        return AuthService.MapUser(user);
    }

    public async Task<UserResponse> UpdateMemberAsync(Guid groupId, Guid userId, UpdateMemberRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.GroupId == groupId)
            ?? throw new KeyNotFoundException("Member not found.");

        user.FirstName = req.FirstName;
        user.LastName = req.LastName;
        user.MiddleName = req.MiddleName;
        await db.SaveChangesAsync();

        return AuthService.MapUser(user);
    }

    public async Task<object> AssignStarostaAsync(Guid groupId, Guid newStarostaId, AuthorSummary actor)
    {
        var newStarosta = await db.Users.FirstOrDefaultAsync(u => u.Id == newStarostaId && u.GroupId == groupId)
            ?? throw new KeyNotFoundException("Member not found.");

        var previousStarosta = await db.Users
            .FirstOrDefaultAsync(u => u.GroupId == groupId && u.Role == UserRole.Starosta);

        Guid? previousStarostaId = null;

        if (previousStarosta is not null)
        {
            previousStarosta.Role = UserRole.Student;
            previousStarostaId = previousStarosta.Id;
        }

        newStarosta.Role = UserRole.Starosta;
        newStarosta.UploadAccess = true;
        await db.SaveChangesAsync();

        await historyService.LogAsync(groupId, HistoryEventKind.RoleChanged, actor,
            $"Призначив(ла) старостою {newStarosta.FirstName} {newStarosta.LastName}");

        return new
        {
            previousStarostaId = previousStarostaId?.ToString(),
            newStarostaId = newStarostaId.ToString()
        };
    }

    public async Task<object> RemoveMemberAsync(Guid groupId, Guid userId, Guid currentUserId, AuthorSummary actor)
    {
        if (userId == currentUserId)
            throw new InvalidOperationException("Studcurator cannot remove themselves.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.GroupId == groupId)
            ?? throw new KeyNotFoundException("Member not found.");

        user.GroupId = null;
        await db.SaveChangesAsync();

        await historyService.LogAsync(groupId, HistoryEventKind.MemberRemoved, actor,
            $"Видалив(ла) {user.FirstName} {user.LastName} з групи");

        return new { removedUserId = userId.ToString() };
    }
}

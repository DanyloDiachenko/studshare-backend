using Microsoft.EntityFrameworkCore;
using StudShare.Api.Data;
using StudShare.Api.DTos;
using StudShare.Api.Entities;

namespace StudShare.Api.Services;

public class GroupService(AppDbContext db, HistoryService historyService)
{
    public async Task<GroupInfoResponse> CreateAsync(CreateGroupRequest req, Guid userId)
    {
        var user = await db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.GroupId is not null)
            throw new InvalidOperationException("User is already in a group.");

        var group = new Group
        {
            Name = req.Name,
            Faculty = req.Faculty,
        };

        db.Groups.Add(group);

        user.GroupId = group.Id;
        user.Role = UserRole.StudCurator;
        user.JoinedAt = DateTime.UtcNow;
        user.UploadAccess = true;

        await db.SaveChangesAsync();

        return MapInfo(group, 1, 0);
    }

    public async Task<GroupInfoResponse> GetCurrentAsync(Guid groupId)
    {
        var g = await db.Groups.FindAsync(groupId)
            ?? throw new KeyNotFoundException("Group not found.");

        var membersCount = await db.Users.CountAsync(u => u.GroupId == groupId);
        var invitationsCount = await db.GroupInvitations
            .CountAsync(i => i.GroupId == groupId && i.ExpiresAt > DateTime.UtcNow);

        return MapInfo(g, membersCount, invitationsCount);
    }

    public async Task<GroupInfoResponse> UpdateAsync(Guid groupId, UpdateGroupRequest req, AuthorSummary actor)
    {
        var g = await db.Groups.FindAsync(groupId)
            ?? throw new KeyNotFoundException("Group not found.");

        g.Name = req.Name;
        g.Faculty = req.Faculty;
        await db.SaveChangesAsync();

        await historyService.LogAsync(groupId, HistoryEventKind.Group, actor,
            $"Оновив(ла) дані групи");

        var membersCount = await db.Users.CountAsync(u => u.GroupId == groupId);
        var invitationsCount = await db.GroupInvitations
            .CountAsync(i => i.GroupId == groupId && i.ExpiresAt > DateTime.UtcNow);

        return MapInfo(g, membersCount, invitationsCount);
    }

    public async Task DeleteAsync(Guid groupId, DeleteGroupRequest req)
    {
        var g = await db.Groups.FindAsync(groupId)
            ?? throw new KeyNotFoundException("Group not found.");

        if (g.Name != req.ConfirmName)
            throw new InvalidOperationException("Group name does not match.");

        await db.Users
            .Where(u => u.GroupId == groupId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.GroupId, (Guid?)null));

        db.Groups.Remove(g);
        await db.SaveChangesAsync();
    }

    private static GroupInfoResponse MapInfo(Group g, int membersCount, int invitationsCount) => new(
        g.Id.ToString(), g.Name, g.Faculty, g.CreatedAt,
        g.StorageUsedBytes, g.StorageLimitBytes, membersCount, invitationsCount);
}

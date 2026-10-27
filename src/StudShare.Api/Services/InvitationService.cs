using Microsoft.EntityFrameworkCore;
using StudShare.Api.Data;
using StudShare.Api.DTos;
using StudShare.Api.Entities;

namespace StudShare.Api.Services;

public class InvitationService(AppDbContext db)
{
    public async Task<GroupInvitationResponse> InviteAsync(Guid groupId, InviteUserRequest req, AuthorSummary actor)
    {
        var existing = await db.GroupInvitations
            .FirstOrDefaultAsync(i => i.GroupId == groupId && i.Email == req.Email);
        if (existing is not null)
            throw new InvalidOperationException("Invitation already exists for this email.");

        var now = DateTime.UtcNow;
        var invitation = new GroupInvitation
        {
            GroupId = groupId,
            Email = req.Email.ToLowerInvariant(),
            InvitedBy = actor,
            InvitedAt = now,
            ExpiresAt = now.AddDays(3),
        };

        db.GroupInvitations.Add(invitation);
        await db.SaveChangesAsync();

        return Map(invitation);
    }

    public async Task<GroupInvitationResponse> ResendAsync(Guid invitationId, Guid groupId)
    {
        var invitation = await db.GroupInvitations
            .FirstOrDefaultAsync(i => i.Id == invitationId && i.GroupId == groupId)
            ?? throw new KeyNotFoundException("Invitation not found.");

        var now = DateTime.UtcNow;
        invitation.InvitedAt = now;
        invitation.ExpiresAt = now.AddDays(3);
        await db.SaveChangesAsync();

        return Map(invitation);
    }

    public async Task<object> CancelAsync(Guid invitationId, Guid groupId)
    {
        var invitation = await db.GroupInvitations
            .FirstOrDefaultAsync(i => i.Id == invitationId && i.GroupId == groupId)
            ?? throw new KeyNotFoundException("Invitation not found.");

        db.GroupInvitations.Remove(invitation);
        await db.SaveChangesAsync();

        return new { cancelledInvitationId = invitationId.ToString() };
    }

    private static GroupInvitationResponse Map(GroupInvitation i) => new(
        i.Id.ToString(), i.GroupId.ToString(), i.Email,
        new AuthorSummaryResponse(i.InvitedBy.Id.ToString(), i.InvitedBy.FirstName,
            i.InvitedBy.LastName, i.InvitedBy.MiddleName, i.InvitedBy.Role.ToString()),
        i.InvitedAt, i.ExpiresAt);
}

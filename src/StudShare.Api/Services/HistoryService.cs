using Microsoft.EntityFrameworkCore;
using StudShare.Api.Data;
using StudShare.Api.DTos;
using StudShare.Api.Entities;

namespace StudShare.Api.Services;

public class HistoryService(AppDbContext db)
{
    public async Task LogAsync(Guid groupId, HistoryEventKind kind, AuthorSummary actor, string text)
    {
        db.HistoryEvents.Add(new HistoryEvent
        {
            GroupId = groupId,
            Kind = kind,
            Actor = actor,
            Text = text,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    public async Task<List<HistoryEventResponse>> GetHistoryAsync(Guid groupId, string? category)
    {
        var query = db.HistoryEvents.Where(h => h.GroupId == groupId);

        query = category switch
        {
            "files"   => query.Where(h => h.Kind == HistoryEventKind.FileAdded
                                       || h.Kind == HistoryEventKind.FileRenamed
                                       || h.Kind == HistoryEventKind.FileDeleted
                                       || h.Kind == HistoryEventKind.FolderCreated),
            "members" => query.Where(h => h.Kind == HistoryEventKind.MemberAdded
                                       || h.Kind == HistoryEventKind.MemberRemoved
                                       || h.Kind == HistoryEventKind.RoleChanged),
            "access"  => query.Where(h => h.Kind == HistoryEventKind.UploadAccess),
            _         => query,
        };

        return await query
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new HistoryEventResponse(
                h.Id.ToString(), h.GroupId.ToString(), h.Kind.ToString(),
                new AuthorSummaryResponse(
                    h.Actor.Id.ToString(), h.Actor.FirstName, h.Actor.LastName,
                    h.Actor.MiddleName, h.Actor.Role.ToString()),
                h.Text, h.CreatedAt))
            .ToListAsync();
    }
}

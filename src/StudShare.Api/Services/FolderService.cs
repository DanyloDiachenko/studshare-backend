using Microsoft.EntityFrameworkCore;
using StudShare.Api.Data;
using StudShare.Api.DTos;
using StudShare.Api.Entities;

namespace StudShare.Api.Services;

public class FolderService(AppDbContext db, HistoryService historyService)
{
    public async Task<FolderResponse> CreateAsync(Guid groupId, CreateFolderRequest req, AuthorSummary actor)
    {
        var parentId = req.ParentId is not null ? Guid.Parse(req.ParentId) : (Guid?)null;

        var duplicate = await db.Folders.AnyAsync(f =>
            f.GroupId == groupId && f.ParentId == parentId && f.Name == req.Name);
        if (duplicate)
            throw new InvalidOperationException("A folder with this name already exists here.");

        var folder = new Folder
        {
            GroupId = groupId,
            ParentId = parentId,
            Name = req.Name,
            Author = actor,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.Folders.Add(folder);
        await db.SaveChangesAsync();

        await historyService.LogAsync(groupId, HistoryEventKind.FolderCreated, actor,
            $"Створив(ла) папку «{req.Name}»");

        return Map(folder);
    }

    public async Task<FolderResponse> RenameAsync(Guid folderId, RenameFolderRequest req, Guid userId, UserRole userRole)
    {
        var folder = await db.Folders.FindAsync(folderId)
            ?? throw new KeyNotFoundException("Folder not found.");

        if (folder.Author.Id != userId && userRole != UserRole.Starosta)
            throw new UnauthorizedAccessException("Not allowed.");

        folder.Name = req.Name;
        folder.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await historyService.LogAsync(folder.GroupId, HistoryEventKind.FileRenamed, folder.Author,
            $"Перейменував(ла) папку на «{req.Name}»");

        return Map(folder);
    }

    public async Task<object> TogglePinAsync(Guid folderId, UserRole userRole)
    {
        if (userRole != UserRole.Starosta)
            throw new UnauthorizedAccessException("Only Starosta can pin folders.");

        var folder = await db.Folders.FindAsync(folderId)
            ?? throw new KeyNotFoundException("Folder not found.");

        folder.IsPinned = !folder.IsPinned;
        await db.SaveChangesAsync();

        return Map(folder);
    }

    public async Task<(List<Guid> deletedIds, long storageUsedBytes)> DeleteAsync(
        Guid folderId, Guid userId, UserRole userRole)
    {
        var folder = await db.Folders.FindAsync(folderId)
            ?? throw new KeyNotFoundException("Folder not found.");

        if (folder.Author.Id != userId
            && userRole != UserRole.Starosta
            && userRole != UserRole.StudCurator)
            throw new UnauthorizedAccessException("Not allowed.");

        var deletedIds = new List<Guid>();
        var freedBytes = await DeleteRecursiveAsync(folderId, deletedIds);

        var group = await db.Groups.FindAsync(folder.GroupId);
        if (group is not null)
        {
            group.StorageUsedBytes -= freedBytes;
            await db.SaveChangesAsync();
        }

        await historyService.LogAsync(folder.GroupId, HistoryEventKind.FileDeleted, folder.Author,
            $"Видалив(ла) папку «{folder.Name}»");

        var storageUsedBytes = group?.StorageUsedBytes ?? 0;
        return (deletedIds, storageUsedBytes);
    }

    private async Task<long> DeleteRecursiveAsync(Guid folderId, List<Guid> deletedIds)
    {
        long freed = 0;

        var subFolders = await db.Folders.Where(f => f.ParentId == folderId).ToListAsync();
        foreach (var sub in subFolders)
            freed += await DeleteRecursiveAsync(sub.Id, deletedIds);

        var files = await db.Files.Where(f => f.FolderId == folderId).ToListAsync();
        freed += files.Sum(f => f.SizeBytes);
        db.Files.RemoveRange(files);

        var folder = await db.Folders.FindAsync(folderId);
        if (folder is not null)
        {
            db.Folders.Remove(folder);
            deletedIds.Add(folderId);
        }

        await db.SaveChangesAsync();
        return freed;
    }

    public static FolderResponse Map(Folder f) => new(
        f.Id.ToString(), f.GroupId.ToString(), f.ParentId?.ToString(),
        f.Name, f.IsPinned,
        new AuthorSummaryResponse(f.Author.Id.ToString(), f.Author.FirstName,
            f.Author.LastName, f.Author.MiddleName, f.Author.Role.ToString()),
        f.CreatedAt, f.UpdatedAt, [], []);
}

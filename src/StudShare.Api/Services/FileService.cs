using Microsoft.EntityFrameworkCore;
using StudShare.Api.Data;
using StudShare.Api.DTos;
using StudShare.Api.Entities;

namespace StudShare.Api.Services;

public class FileService(AppDbContext db, HistoryService historyService, IWebHostEnvironment env)
{
    public async Task<(List<FolderResponse> folders, List<FileResponse> files)> GetSnapshotAsync(Guid groupId)
    {
        var folders = await db.Folders
            .Where(f => f.GroupId == groupId)
            .ToListAsync();

        var files = await db.Files
            .Where(f => f.GroupId == groupId)
            .ToListAsync();

        return (folders.Select(FolderService.Map).ToList(), files.Select(Map).ToList());
    }

    public async Task<(List<FolderResponse> folders, List<FileResponse> files)> SearchAsync(Guid groupId, string q)
    {
        var folders = await db.Folders
            .Where(f => f.GroupId == groupId && EF.Functions.ILike(f.Name, $"%{q}%"))
            .ToListAsync();

        var files = await db.Files
            .Where(f => f.GroupId == groupId && EF.Functions.ILike(f.Name, $"%{q}%"))
            .ToListAsync();

        return (folders.Select(FolderService.Map).ToList(), files.Select(Map).ToList());
    }

    public async Task<FileResponse> UploadAsync(Guid groupId, IFormFile file, string? folderId, AuthorSummary actor)
    {
        var group = await db.Groups.FindAsync(groupId)
            ?? throw new KeyNotFoundException("Group not found.");

        if (group.StorageUsedBytes + file.Length > group.StorageLimitBytes)
            throw new InvalidOperationException("Storage limit exceeded.");

        var extension = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant();
        var category = GetCategory(extension);
        var fileName = $"{Guid.NewGuid()}.{extension}";
        var uploadDir = Path.Combine(env.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploadDir);
        var filePath = Path.Combine(uploadDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var entry = new Entities.File
        {
            GroupId = groupId,
            FolderId = folderId is not null ? Guid.Parse(folderId) : null,
            Name = Path.GetFileNameWithoutExtension(file.FileName),
            Extension = extension,
            Category = category,
            SizeBytes = file.Length,
            Url = $"/uploads/{fileName}",
            Author = actor,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.Files.Add(entry);
        group.StorageUsedBytes += file.Length;
        await db.SaveChangesAsync();

        await historyService.LogAsync(groupId, HistoryEventKind.FileAdded, actor,
            $"Завантажив(ла) файл «{entry.Name}.{extension}»");

        return Map(entry);
    }

    public async Task<(string path, string contentType, string fileName)> DownloadAsync(Guid fileId)
    {
        var entry = await db.Files.FindAsync(fileId)
            ?? throw new KeyNotFoundException("File not found.");

        var path = Path.Combine(env.ContentRootPath, entry.Url.TrimStart('/'));
        var contentType = GetContentType(entry.Extension);
        return (path, contentType, $"{entry.Name}.{entry.Extension}");
    }

    public async Task<FileResponse> RenameAsync(Guid fileId, RenameFileRequest req, Guid userId, UserRole userRole)
    {
        var entry = await db.Files.FindAsync(fileId)
            ?? throw new KeyNotFoundException("File not found.");

        if (entry.Author.Id != userId && userRole != UserRole.Starosta)
            throw new UnauthorizedAccessException("Not allowed.");

        entry.Name = req.Name;
        entry.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await historyService.LogAsync(entry.GroupId, HistoryEventKind.FileRenamed, entry.Author,
            $"Перейменував(ла) файл на «{req.Name}»");

        return Map(entry);
    }

    public async Task<FileResponse> TogglePinAsync(Guid fileId, UserRole userRole)
    {
        if (userRole != UserRole.Starosta)
            throw new UnauthorizedAccessException("Only Starosta can pin files.");

        var entry = await db.Files.FindAsync(fileId)
            ?? throw new KeyNotFoundException("File not found.");

        entry.IsPinned = !entry.IsPinned;
        await db.SaveChangesAsync();
        return Map(entry);
    }

    public async Task<(Guid deletedFileId, long storageUsedBytes)> DeleteAsync(
        Guid fileId, Guid userId, UserRole userRole)
    {
        var entry = await db.Files.FindAsync(fileId)
            ?? throw new KeyNotFoundException("File not found.");

        if (entry.Author.Id != userId
            && userRole != UserRole.Starosta
            && userRole != UserRole.StudCurator)
            throw new UnauthorizedAccessException("Not allowed.");

        var group = await db.Groups.FindAsync(entry.GroupId);
        if (group is not null)
            group.StorageUsedBytes -= entry.SizeBytes;

        db.Files.Remove(entry);
        await db.SaveChangesAsync();

        await historyService.LogAsync(entry.GroupId, HistoryEventKind.FileDeleted, entry.Author,
            $"Видалив(ла) файл «{entry.Name}»");

        return (fileId, group?.StorageUsedBytes ?? 0);
    }

    private static FileCategory GetCategory(string ext) => ext switch
    {
        "pdf"              => FileCategory.Pdf,
        "docx" or "doc"   => FileCategory.Docx,
        "pptx" or "ppt"   => FileCategory.Pptx,
        "jpg" or "jpeg"
            or "png" or "gif"
            or "webp"      => FileCategory.Image,
        "mp4" or "mov"
            or "avi"       => FileCategory.Video,
        _                  => FileCategory.Other,
    };

    private static string GetContentType(string ext) => ext switch
    {
        "pdf"  => "application/pdf",
        "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "jpg" or "jpeg" => "image/jpeg",
        "png"  => "image/png",
        "mp4"  => "video/mp4",
        _      => "application/octet-stream",
    };

    public static FileResponse Map(Entities.File f) => new(
        f.Id.ToString(), f.GroupId.ToString(), f.FolderId?.ToString(),
        f.Name, f.Extension, f.Category.ToString(), f.SizeBytes, f.Url, f.IsPinned,
        new AuthorSummaryResponse(f.Author.Id.ToString(), f.Author.FirstName,
            f.Author.LastName, f.Author.MiddleName, f.Author.Role.ToString()),
        f.CreatedAt, f.UpdatedAt);
}

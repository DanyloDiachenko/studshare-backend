namespace StudShare.Api.DTos;

public record CreateFolderRequest(
    string Name,
    string? ParentId
);

public record RenameFolderRequest(
    string Name
);

public record FolderResponse(
    string Id,
    string GroupId,
    string? ParentId,
    string Name,
    bool IsPinned,
    AuthorSummaryResponse Author,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<FolderResponse> Folders,
    List<FileResponse> Files
);

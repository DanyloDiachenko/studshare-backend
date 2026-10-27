namespace StudShare.Api.DTos;

public record RenameFileRequest(
    string Name
);

public record FileResponse(
    string Id,
    string GroupId,
    string? FolderId,
    string Name,
    string Extension,
    string Category,
    long SizeBytes,
    string Url,
    bool IsPinned,
    AuthorSummaryResponse Author,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

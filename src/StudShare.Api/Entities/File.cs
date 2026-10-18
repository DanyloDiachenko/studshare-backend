namespace StudShare.Api.Entities;

public enum FileCategory
{
    Pdf,
    Docx,
    Pptx,
    Image,
    Video,
    Other
}

public class File
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public Guid? FolderId { get; set; } = null;
    public string Name { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public FileCategory Category { get; set; } = FileCategory.Other;
    public long SizeBytes { get; set; } = 0;
    public string Url { get; set; } = string.Empty;
    public bool IsPinned { get; set; } = false;
    public AuthorSummary Author { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
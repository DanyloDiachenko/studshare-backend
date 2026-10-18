namespace StudShare.Api.Entities;

public enum HistoryEventKind
{
    FileAdded,
    FolderCreated,
    FileRenamed,
    FileDeleted,
    MemberAdded,
    MemberRemoved,
    RoleChanged,
    UploadAccess,
    Group
}

public class HistoryEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public HistoryEventKind Kind { get; set; }
    public AuthorSummary Actor { get; set; } = null!;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
namespace StudShare.Api.Entities;

public class Folder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public Guid? ParentId { get; set; } = null;
    public string Name { get; set; } = string.Empty;
    public bool IsPinned { get; set; } = false;
    public List<File> Files { get; set; } = new List<File>();
    public List<Folder> Folders { get; set; } = new List<Folder>();
    public AuthorSummary Author { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
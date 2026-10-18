namespace StudShare.Api.Entities;

public class Group
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Faculty { get; set; } = String.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long StorageUsedBytes { get; set; } = 0;
    public long StorageLimitBytes { get; set; } = 1000000000;
    public List<User> Members { get; set; } = new List<User>();
    public List<GroupInvitation> Invitations { get; set; } = new List<GroupInvitation>();
    public List<HistoryEvent> History { get; set; } = new List<HistoryEvent>();
    public List<Folder> Folders { get; set; } = new List<Folder>();
    public List<File> Files { get; set; } = new List<File>();
}
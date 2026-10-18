namespace StudShare.Api.Entities;

public class GroupInvitation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public string Email { get; set; } = string.Empty;
    public AuthorSummary InvitedBy { get; set; } = null!;
    public DateTime InvitedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
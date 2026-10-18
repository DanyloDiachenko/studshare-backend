namespace StudShare.Api.Entities;

public enum UserRole
{
    StudCurator,
    Starosta,
    Student
}

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid? GroupId { get; set; }
    public bool UploadAccess { get; set; } = true;
    public DateTime JoinedAt { get; set; }
    public DateTime? InvitedAt { get; set; }
}

public class AuthorSummary
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public UserRole Role { get; set; }
}
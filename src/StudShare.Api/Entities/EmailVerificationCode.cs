namespace StudShare.Api.Entities;

public enum EmailCodePurpose
{
    EmailConfirmation,
    PasswordReset
}

public class EmailVerificationCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public EmailCodePurpose Purpose { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
}

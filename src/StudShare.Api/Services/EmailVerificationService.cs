using Microsoft.EntityFrameworkCore;
using StudShare.Api.Data;
using StudShare.Api.Entities;

namespace StudShare.Api.Services;

public class EmailVerificationService(AppDbContext db, EmailService emailService)
{
    private static string GenerateCode() =>
        Random.Shared.Next(100_000, 999_999).ToString();

    public async Task SendConfirmationAsync(string email)
    {
        await InvalidateExistingAsync(email, EmailCodePurpose.EmailConfirmation);

        var code = GenerateCode();
        db.EmailVerificationCodes.Add(new EmailVerificationCode
        {
            Email = email.ToLowerInvariant(),
            Code = code,
            Purpose = EmailCodePurpose.EmailConfirmation,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
        });
        await db.SaveChangesAsync();
        await emailService.SendVerificationCodeAsync(email, code);
    }

    public async Task SendPasswordResetAsync(string email)
    {
        var userExists = await db.Users.AnyAsync(u => u.Email == email.ToLowerInvariant());
        if (!userExists) return; // silent – don't reveal whether email is registered

        await InvalidateExistingAsync(email, EmailCodePurpose.PasswordReset);

        var code = GenerateCode();
        db.EmailVerificationCodes.Add(new EmailVerificationCode
        {
            Email = email.ToLowerInvariant(),
            Code = code,
            Purpose = EmailCodePurpose.PasswordReset,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
        });
        await db.SaveChangesAsync();
        await emailService.SendPasswordResetCodeAsync(email, code);
    }

    public async Task<bool> VerifyEmailConfirmationAsync(string email, string code)
    {
        var entry = await FindValidAsync(email, code, EmailCodePurpose.EmailConfirmation);
        if (entry is null) return false;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
        if (user is null) return false;

        user.IsEmailVerified = true;
        entry.IsUsed = true;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResetPasswordAsync(string email, string code, string newPassword)
    {
        var entry = await FindValidAsync(email, code, EmailCodePurpose.PasswordReset);
        if (entry is null) return false;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
        if (user is null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        entry.IsUsed = true;
        await db.SaveChangesAsync();
        return true;
    }

    private async Task InvalidateExistingAsync(string email, EmailCodePurpose purpose)
    {
        var existing = await db.EmailVerificationCodes
            .Where(c => c.Email == email.ToLowerInvariant() && c.Purpose == purpose && !c.IsUsed)
            .ToListAsync();
        foreach (var c in existing) c.IsUsed = true;
    }

    private Task<EmailVerificationCode?> FindValidAsync(string email, string code, EmailCodePurpose purpose) =>
        db.EmailVerificationCodes.FirstOrDefaultAsync(c =>
            c.Email == email.ToLowerInvariant() &&
            c.Code == code &&
            c.Purpose == purpose &&
            !c.IsUsed &&
            c.ExpiresAt > DateTime.UtcNow);
}

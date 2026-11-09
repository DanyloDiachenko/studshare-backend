using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StudShare.Api.Data;
using StudShare.Api.Entities;
using StudShare.Api.Services;
using Xunit;

namespace StudShare.UnitTests.Components;

public class EmailVerificationServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SqliteConnection _connection;
    private readonly EmailVerificationService _verificationService;
    private readonly TestEmailService _emailService;

    public EmailVerificationServiceTests()
    {
        (_db, _connection) = TestDbContextFactory.CreateInMemoryDbContext();
        var config = new ConfigurationBuilder().Build();
        _emailService = new TestEmailService(config);
        _verificationService = new EmailVerificationService(_db, _emailService);
    }

    [Fact]
    public async Task SendConfirmationAsync_ShouldCreateCodeAndCallEmailService()
    {
        const string email = "student@kpi.ua";
        await _verificationService.SendConfirmationAsync(email);

        Assert.Single(_emailService.SentEmails);
        Assert.Equal(email, _emailService.SentEmails[0].To);

        var codeInDb = await _db.EmailVerificationCodes.FirstOrDefaultAsync(c => c.Email == email);
        Assert.NotNull(codeInDb);
        Assert.Equal(6, codeInDb.Code.Length);
        Assert.False(codeInDb.IsUsed);
        Assert.True(codeInDb.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task VerifyEmailConfirmationAsync_ValidCode_ShouldVerifyAndMarkUsed()
    {
        const string email = "student@kpi.ua";
        var user = new User
        {
            Email = email,
            FirstName = "Іван",
            LastName = "Студент",
            IsEmailVerified = false
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await _verificationService.SendConfirmationAsync(email);
        var codeInDb = await _db.EmailVerificationCodes.FirstAsync(c => c.Email == email);

        var verified = await _verificationService.VerifyEmailConfirmationAsync(email, codeInDb.Code);
        Assert.True(verified);

        var updatedUser = await _db.Users.FindAsync(user.Id);
        Assert.True(updatedUser!.IsEmailVerified);

        var secondAttempt = await _verificationService.VerifyEmailConfirmationAsync(email, codeInDb.Code);
        Assert.False(secondAttempt);
    }

    [Fact]
    public async Task VerifyEmailConfirmationAsync_ExpiredCode_ShouldFail()
    {
        const string email = "expired@kpi.ua";
        var user = new User { Email = email, FirstName = "Іван", LastName = "Студент" };
        _db.Users.Add(user);

        var expiredCode = new EmailVerificationCode
        {
            Email = email,
            Code = "123456",
            Purpose = EmailCodePurpose.EmailConfirmation,
            CreatedAt = DateTime.UtcNow.AddMinutes(-30),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-15),
            IsUsed = false
        };
        _db.EmailVerificationCodes.Add(expiredCode);
        await _db.SaveChangesAsync();

        var result = await _verificationService.VerifyEmailConfirmationAsync(email, "123456");
        Assert.False(result);
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidCode_ShouldUpdatePasswordHash()
    {
        const string email = "reset@kpi.ua";
        var user = new User
        {
            Email = email,
            FirstName = "Іван",
            LastName = "Студент",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!")
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await _verificationService.SendPasswordResetAsync(email);
        var codeInDb = await _db.EmailVerificationCodes.FirstAsync(c => c.Email == email && c.Purpose == EmailCodePurpose.PasswordReset);

        var success = await _verificationService.ResetPasswordAsync(email, codeInDb.Code, "NewPassword999!");
        Assert.True(success);

        var updatedUser = await _db.Users.FindAsync(user.Id);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword999!", updatedUser!.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify("OldPassword123!", updatedUser.PasswordHash));
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private class TestEmailService(IConfiguration config) : EmailService(config)
    {
        public List<(string To, string Subject, string Body)> SentEmails { get; } = new();

        public override Task SendAsync(string to, string subject, string htmlBody)
        {
            SentEmails.Add((to, subject, htmlBody));
            return Task.CompletedTask;
        }
    }
}

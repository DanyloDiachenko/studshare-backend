using Xunit;

namespace StudShare.UnitTests.Unit;

public class PasswordSecurityTests
{
    [Fact]
    public void BCrypt_HashPassword_ShouldProduceValidHash()
    {
        const string password = "SecretPassword123!";
        var hash = BCrypt.Net.BCrypt.HashPassword(password);

        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.NotEqual(password, hash);
        Assert.StartsWith("$2", hash);
    }

    [Fact]
    public void BCrypt_VerifyPassword_ShouldVerifyCorrectPassword()
    {
        const string password = "SecretPassword123!";
        var hash = BCrypt.Net.BCrypt.HashPassword(password);

        var isValid = BCrypt.Net.BCrypt.Verify(password, hash);
        Assert.True(isValid);
    }

    [Fact]
    public void BCrypt_VerifyPassword_ShouldRejectWrongPassword()
    {
        const string password = "SecretPassword123!";
        var hash = BCrypt.Net.BCrypt.HashPassword(password);

        var isValid = BCrypt.Net.BCrypt.Verify("WrongPassword999!", hash);
        Assert.False(isValid);
    }

    [Fact]
    public void BCrypt_HashPassword_ShouldGenerateUniqueSaltEveryTime()
    {
        const string password = "SamePassword123!";
        var hash1 = BCrypt.Net.BCrypt.HashPassword(password);
        var hash2 = BCrypt.Net.BCrypt.HashPassword(password);

        Assert.NotEqual(hash1, hash2);
        Assert.True(BCrypt.Net.BCrypt.Verify(password, hash1));
        Assert.True(BCrypt.Net.BCrypt.Verify(password, hash2));
    }
}

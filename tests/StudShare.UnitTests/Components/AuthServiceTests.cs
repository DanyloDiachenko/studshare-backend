using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StudShare.Api.Data;
using StudShare.Api.DTos;
using StudShare.Api.Entities;
using StudShare.Api.Services;
using Xunit;

namespace StudShare.UnitTests.Components;

public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SqliteConnection _connection;
    private readonly AuthService _authService;
    private readonly HistoryService _historyService;

    public AuthServiceTests()
    {
        Environment.SetEnvironmentVariable("JWT_SECRET", "super-secret-key-change-in-production-min-32-chars");

        (_db, _connection) = TestDbContextFactory.CreateInMemoryDbContext();
        _historyService = new HistoryService(_db);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "StudShare",
                ["Jwt:Audience"] = "StudShare"
            })
            .Build();

        _authService = new AuthService(_db, config, _historyService);
    }

    [Fact]
    public async Task RegisterAsync_NewUser_ShouldCreateUserSuccessfully()
    {
        var req = new SignUpRequest("ivan@kpi.ua", "Pass12345!", "Іван", "Іваненко", "Петрович");

        var (authRes, userRes) = await _authService.RegisterAsync(req);

        Assert.NotNull(authRes.Token);
        Assert.Equal("ivan@kpi.ua", userRes.Email);
        Assert.Equal("Student", userRes.Role);

        var dbUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == "ivan@kpi.ua");
        Assert.NotNull(dbUser);
        Assert.True(BCrypt.Net.BCrypt.Verify("Pass12345!", dbUser.PasswordHash));
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ShouldThrowInvalidOperationException()
    {
        var req = new SignUpRequest("duplicate@kpi.ua", "Pass12345!", "Іван", "Іваненко", null);
        await _authService.RegisterAsync(req);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(req));
    }

    [Fact]
    public async Task RegisterAsync_WithActiveInvite_ShouldJoinGroupAndRemoveInvite()
    {
        var groupId = Guid.NewGuid();
        var invite = new GroupInvitation
        {
            GroupId = groupId,
            Email = "invited@kpi.ua",
            InvitedBy = new AuthorSummary { Id = Guid.NewGuid(), FirstName = "Куратор", LastName = "Тест", Role = UserRole.StudCurator },
            InvitedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(2)
        };
        _db.GroupInvitations.Add(invite);
        await _db.SaveChangesAsync();

        var req = new SignUpRequest("invited@kpi.ua", "Pass12345!", "Новий", "Студент", null);
        var (_, userRes) = await _authService.RegisterAsync(req);

        Assert.Equal(groupId.ToString(), userRes.GroupId);

        var dbInvite = await _db.GroupInvitations.FirstOrDefaultAsync(i => i.Email == "invited@kpi.ua");
        Assert.Null(dbInvite);

        var history = await _db.HistoryEvents.FirstOrDefaultAsync(h => h.GroupId == groupId);
        Assert.NotNull(history);
        Assert.Equal(HistoryEventKind.MemberAdded, history.Kind);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ShouldReturnToken()
    {
        var req = new SignUpRequest("login@kpi.ua", "CorrectPass123!", "Іван", "Іваненко", null);
        await _authService.RegisterAsync(req);

        var (authRes, userRes) = await _authService.LoginAsync(new SignInRequest("login@kpi.ua", "CorrectPass123!"));

        Assert.NotNull(authRes.Token);
        Assert.Equal("login@kpi.ua", userRes.Email);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ShouldThrowUnauthorizedAccessException()
    {
        var req = new SignUpRequest("wrongpass@kpi.ua", "CorrectPass123!", "Іван", "Іваненко", null);
        await _authService.RegisterAsync(req);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.LoginAsync(new SignInRequest("wrongpass@kpi.ua", "WrongPassword!")));
    }

    [Fact]
    public async Task LoginAsync_NonExistentEmail_ShouldThrowUnauthorizedAccessException()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.LoginAsync(new SignInRequest("nobody@kpi.ua", "AnyPassword!")));
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}

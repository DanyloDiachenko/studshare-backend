using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StudShare.Api.Data;
using StudShare.Api.DTos;
using StudShare.Api.Entities;
using StudShare.Api.Services;
using Xunit;

namespace StudShare.UnitTests.Components;

public class MemberServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SqliteConnection _connection;
    private readonly MemberService _memberService;
    private readonly HistoryService _historyService;

    public MemberServiceTests()
    {
        (_db, _connection) = TestDbContextFactory.CreateInMemoryDbContext();
        _historyService = new HistoryService(_db);
        _memberService = new MemberService(_db, _historyService);
    }

    [Fact]
    public async Task AssignStarostaAsync_ShouldPromoteNewAndDemotePreviousStarosta()
    {
        var groupId = Guid.NewGuid();
        var oldStarosta = new User
        {
            Email = "old@kpi.ua",
            FirstName = "Старий",
            LastName = "Староста",
            Role = UserRole.Starosta,
            GroupId = groupId,
            UploadAccess = true
        };
        var newStarostaCandidate = new User
        {
            Email = "new@kpi.ua",
            FirstName = "Новий",
            LastName = "Староста",
            Role = UserRole.Student,
            GroupId = groupId,
            UploadAccess = false
        };

        _db.Users.AddRange(oldStarosta, newStarostaCandidate);
        await _db.SaveChangesAsync();

        var actor = new AuthorSummary { Id = Guid.NewGuid(), FirstName = "Куратор", LastName = "Тест", Role = UserRole.StudCurator };
        await _memberService.AssignStarostaAsync(groupId, newStarostaCandidate.Id, actor);

        var updatedOld = await _db.Users.FindAsync(oldStarosta.Id);
        var updatedNew = await _db.Users.FindAsync(newStarostaCandidate.Id);

        Assert.Equal(UserRole.Student, updatedOld!.Role);
        Assert.Equal(UserRole.Starosta, updatedNew!.Role);
        Assert.True(updatedNew.UploadAccess);

        var history = await _db.HistoryEvents.FirstOrDefaultAsync(h => h.GroupId == groupId && h.Kind == HistoryEventKind.RoleChanged);
        Assert.NotNull(history);
        Assert.Contains("Призначив(ла) старостою", history.Text);
    }

    [Fact]
    public async Task SetUploadAccessAsync_Student_ShouldChangeAccessAndLogHistory()
    {
        var groupId = Guid.NewGuid();
        var student = new User
        {
            Email = "student@kpi.ua",
            FirstName = "Іван",
            LastName = "Студент",
            Role = UserRole.Student,
            GroupId = groupId,
            UploadAccess = true
        };
        _db.Users.Add(student);
        await _db.SaveChangesAsync();

        var actor = new AuthorSummary { Id = Guid.NewGuid(), FirstName = "Староста", LastName = "Тест", Role = UserRole.Starosta };
        var res = await _memberService.SetUploadAccessAsync(groupId, student.Id, new SetUploadAccessRequest(false), actor);

        Assert.False(res.UploadAccess);

        var dbUser = await _db.Users.FindAsync(student.Id);
        Assert.False(dbUser!.UploadAccess);

        var history = await _db.HistoryEvents.FirstOrDefaultAsync(h => h.GroupId == groupId && h.Kind == HistoryEventKind.UploadAccess);
        Assert.NotNull(history);
        Assert.Contains("вимкнув(ла) завантаження", history.Text);
    }

    [Fact]
    public async Task SetUploadAccessAsync_NonStudent_ShouldThrowInvalidOperationException()
    {
        var groupId = Guid.NewGuid();
        var starosta = new User
        {
            Email = "starosta@kpi.ua",
            FirstName = "Петро",
            LastName = "Староста",
            Role = UserRole.Starosta,
            GroupId = groupId,
            UploadAccess = true
        };
        _db.Users.Add(starosta);
        await _db.SaveChangesAsync();

        var actor = new AuthorSummary { Id = Guid.NewGuid(), FirstName = "Куратор", LastName = "Тест", Role = UserRole.StudCurator };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _memberService.SetUploadAccessAsync(groupId, starosta.Id, new SetUploadAccessRequest(false), actor));
    }

    [Fact]
    public async Task RemoveMemberAsync_StudCuratorSelfRemoval_ShouldThrow()
    {
        var groupId = Guid.NewGuid();
        var curatorId = Guid.NewGuid();
        var actor = new AuthorSummary { Id = curatorId, FirstName = "Куратор", LastName = "Тест", Role = UserRole.StudCurator };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _memberService.RemoveMemberAsync(groupId, curatorId, curatorId, actor));
    }

    [Fact]
    public async Task RemoveMemberAsync_OtherStudent_ShouldDetachAndLogHistory()
    {
        var groupId = Guid.NewGuid();
        var curatorId = Guid.NewGuid();
        var student = new User
        {
            Email = "student@kpi.ua",
            FirstName = "Іван",
            LastName = "Студент",
            Role = UserRole.Student,
            GroupId = groupId
        };
        _db.Users.Add(student);
        await _db.SaveChangesAsync();

        var actor = new AuthorSummary { Id = curatorId, FirstName = "Куратор", LastName = "Тест", Role = UserRole.StudCurator };
        await _memberService.RemoveMemberAsync(groupId, student.Id, curatorId, actor);

        var dbStudent = await _db.Users.FindAsync(student.Id);
        Assert.Null(dbStudent!.GroupId);

        var history = await _db.HistoryEvents.FirstOrDefaultAsync(h => h.GroupId == groupId && h.Kind == HistoryEventKind.MemberRemoved);
        Assert.NotNull(history);
        Assert.Contains("Видалив(ла)", history.Text);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}

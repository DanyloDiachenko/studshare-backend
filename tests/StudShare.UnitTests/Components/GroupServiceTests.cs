using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StudShare.Api.Data;
using StudShare.Api.DTos;
using StudShare.Api.Entities;
using StudShare.Api.Services;
using Xunit;

namespace StudShare.UnitTests.Components;

public class GroupServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SqliteConnection _connection;
    private readonly GroupService _groupService;
    private readonly HistoryService _historyService;

    public GroupServiceTests()
    {
        (_db, _connection) = TestDbContextFactory.CreateInMemoryDbContext();
        _historyService = new HistoryService(_db);
        _groupService = new GroupService(_db, _historyService);
    }

    [Fact]
    public async Task CreateAsync_ValidUser_ShouldCreateGroupAndSetStudCurator()
    {
        var user = new User
        {
            Email = "curator@kpi.ua",
            FirstName = "Олег",
            LastName = "Куратор",
            Role = UserRole.Student,
            GroupId = null
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var res = await _groupService.CreateAsync(new CreateGroupRequest("ІП-44", "ФІОТ"), user.Id);

        Assert.Equal("ІП-44", res.Name);
        Assert.Equal("ФІОТ", res.Faculty);

        var updatedUser = await _db.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser!.GroupId);
        Assert.Equal(UserRole.StudCurator, updatedUser.Role);
        Assert.True(updatedUser.UploadAccess);
    }

    [Fact]
    public async Task CreateAsync_UserAlreadyInGroup_ShouldThrowInvalidOperationException()
    {
        var user = new User
        {
            Email = "existing@kpi.ua",
            FirstName = "Студент",
            LastName = "Груповий",
            Role = UserRole.Student,
            GroupId = Guid.NewGuid()
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _groupService.CreateAsync(new CreateGroupRequest("Нова група", "ФІОТ"), user.Id));
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateGroupAndLogHistory()
    {
        var group = new Group { Name = "ІП-41", Faculty = "ФІОТ" };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var actor = new AuthorSummary { Id = Guid.NewGuid(), FirstName = "Куратор", LastName = "Тест", Role = UserRole.StudCurator };
        var updated = await _groupService.UpdateAsync(group.Id, new UpdateGroupRequest("ІП-44", "ФІОТ (оновлено)"), actor);

        Assert.Equal("ІП-44", updated.Name);
        Assert.Equal("ФІОТ (оновлено)", updated.Faculty);

        var history = await _db.HistoryEvents.FirstOrDefaultAsync(h => h.GroupId == group.Id);
        Assert.NotNull(history);
        Assert.Equal(HistoryEventKind.Group, history.Kind);
    }

    [Fact]
    public async Task DeleteAsync_MismatchedConfirmName_ShouldThrow()
    {
        var group = new Group { Name = "ІП-44", Faculty = "ФІОТ" };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _groupService.DeleteAsync(group.Id, new DeleteGroupRequest("Неправильна назва")));
    }

    [Fact]
    public async Task DeleteAsync_MatchingName_ShouldRemoveGroupAndDetachMembers()
    {
        var group = new Group { Name = "ІП-44", Faculty = "ФІОТ" };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var member = new User
        {
            Email = "member@kpi.ua",
            FirstName = "Учасник",
            LastName = "Групи",
            GroupId = group.Id
        };
        _db.Users.Add(member);
        await _db.SaveChangesAsync();

        await _groupService.DeleteAsync(group.Id, new DeleteGroupRequest("ІП-44"));

        var dbGroup = await _db.Groups.FindAsync(group.Id);
        Assert.Null(dbGroup);

        var dbMember = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == member.Id);
        Assert.Null(dbMember!.GroupId);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}

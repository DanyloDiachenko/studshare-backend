using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StudShare.Api.Data;
using StudShare.Api.DTos;
using StudShare.Api.Entities;
using StudShare.Api.Services;
using Xunit;

namespace StudShare.UnitTests.Components;

public class FolderServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SqliteConnection _connection;
    private readonly FolderService _folderService;
    private readonly HistoryService _historyService;

    public FolderServiceTests()
    {
        (_db, _connection) = TestDbContextFactory.CreateInMemoryDbContext();
        _historyService = new HistoryService(_db);
        _folderService = new FolderService(_db, _historyService);
    }

    [Fact]
    public async Task CreateAsync_ValidFolder_ShouldCreateAndLogHistory()
    {
        var groupId = Guid.NewGuid();
        var actor = new AuthorSummary { Id = Guid.NewGuid(), FirstName = "Іван", LastName = "Студент", Role = UserRole.Student };

        var folder = await _folderService.CreateAsync(groupId, new CreateFolderRequest("Фізика", null), actor);

        Assert.Equal("Фізика", folder.Name);
        Assert.Null(folder.ParentId);

        var history = await _db.HistoryEvents.FirstOrDefaultAsync(h => h.GroupId == groupId && h.Kind == HistoryEventKind.FolderCreated);
        Assert.NotNull(history);
        Assert.Contains("Створив(ла) папку", history.Text);
    }

    [Fact]
    public async Task CreateAsync_DuplicateNameInSameParent_ShouldThrow()
    {
        var groupId = Guid.NewGuid();
        var actor = new AuthorSummary { Id = Guid.NewGuid(), FirstName = "Іван", LastName = "Студент", Role = UserRole.Student };

        await _folderService.CreateAsync(groupId, new CreateFolderRequest("Фізика", null), actor);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _folderService.CreateAsync(groupId, new CreateFolderRequest("Фізика", null), actor));
    }

    [Fact]
    public async Task TogglePinAsync_OnlyStarosta_ShouldTogglePin()
    {
        var groupId = Guid.NewGuid();
        var author = new AuthorSummary { Id = Guid.NewGuid(), FirstName = "Іван", LastName = "Студент", Role = UserRole.Student };
        var folder = new Folder { GroupId = groupId, Name = "Математика", Author = author };
        _db.Folders.Add(folder);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _folderService.TogglePinAsync(folder.Id, UserRole.Student));

        var pinned = (FolderResponse)await _folderService.TogglePinAsync(folder.Id, UserRole.Starosta);
        Assert.True(pinned.IsPinned);

        var unpinned = (FolderResponse)await _folderService.TogglePinAsync(folder.Id, UserRole.Starosta);
        Assert.False(unpinned.IsPinned);
    }

    [Fact]
    public async Task DeleteAsync_Recursive_ShouldDeleteSubfoldersAndFilesAndFreeStorage()
    {
        var groupId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        AuthorSummary MakeAuthor() => new() { Id = authorId, FirstName = "Іван", LastName = "Студент", Role = UserRole.Student };

        var group = new Group { Id = groupId, Name = "ІП-44", Faculty = "ФІОТ", StorageUsedBytes = 5000 };
        _db.Groups.Add(group);

        var parentFolder = new Folder { Id = Guid.NewGuid(), GroupId = groupId, Name = "Батьківська", Author = MakeAuthor() };
        var childFolder = new Folder { Id = Guid.NewGuid(), GroupId = groupId, ParentId = parentFolder.Id, Name = "Дочірня", Author = MakeAuthor() };
        _db.Folders.AddRange(parentFolder, childFolder);

        var file1 = new StudShare.Api.Entities.File
        {
            GroupId = groupId,
            FolderId = parentFolder.Id,
            Name = "file1",
            Extension = "pdf",
            SizeBytes = 2000,
            Author = MakeAuthor()
        };
        var file2 = new StudShare.Api.Entities.File
        {
            GroupId = groupId,
            FolderId = childFolder.Id,
            Name = "file2",
            Extension = "docx",
            SizeBytes = 3000,
            Author = MakeAuthor()
        };
        _db.Files.AddRange(file1, file2);
        await _db.SaveChangesAsync();

        var (deletedIds, newStorageUsed) = await _folderService.DeleteAsync(parentFolder.Id, authorId, UserRole.Student);

        Assert.Equal(2, deletedIds.Count);
        Assert.Equal(0, newStorageUsed);

        Assert.False(await _db.Folders.AnyAsync(f => f.Id == parentFolder.Id || f.Id == childFolder.Id));
        Assert.False(await _db.Files.AnyAsync(f => f.Id == file1.Id || f.Id == file2.Id));
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}

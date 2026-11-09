using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using StudShare.Api.Data;
using StudShare.Api.DTos;
using StudShare.Api.Entities;
using StudShare.Api.Services;
using Xunit;

namespace StudShare.UnitTests.Components;

public class FileServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SqliteConnection _connection;
    private readonly FileService _fileService;
    private readonly HistoryService _historyService;
    private readonly TestWebHostEnvironment _env;

    public FileServiceTests()
    {
        (_db, _connection) = TestDbContextFactory.CreateInMemoryDbContext();
        _historyService = new HistoryService(_db);
        _env = new TestWebHostEnvironment();
        _fileService = new FileService(_db, _historyService, _env);
    }

    [Fact]
    public async Task UploadAsync_ExceedingStorageLimit_ShouldThrowInvalidOperationException()
    {
        var groupId = Guid.NewGuid();
        var group = new Group
        {
            Id = groupId,
            Name = "ІП-44",
            Faculty = "ФІОТ",
            StorageUsedBytes = 900,
            StorageLimitBytes = 1000
        };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var formFile = CreateFormFile("lecture.pdf", new byte[200]);
        var actor = new AuthorSummary { Id = Guid.NewGuid(), FirstName = "Студент", LastName = "Тест", Role = UserRole.Student };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _fileService.UploadAsync(groupId, formFile, null, actor));
    }

    [Fact]
    public async Task UploadAsync_ValidFile_ShouldSaveFileAndUpdateGroupStorage()
    {
        var groupId = Guid.NewGuid();
        var group = new Group
        {
            Id = groupId,
            Name = "ІП-44",
            Faculty = "ФІОТ",
            StorageUsedBytes = 0,
            StorageLimitBytes = 1_000_000
        };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var formFile = CreateFormFile("lecture.pdf", new byte[500]);
        var actor = new AuthorSummary { Id = Guid.NewGuid(), FirstName = "Студент", LastName = "Тест", Role = UserRole.Student };

        var result = await _fileService.UploadAsync(groupId, formFile, null, actor);

        Assert.Equal("lecture", result.Name);
        Assert.Equal("pdf", result.Extension);
        Assert.Equal(500, result.SizeBytes);

        var dbGroup = await _db.Groups.FindAsync(groupId);
        Assert.Equal(500, dbGroup!.StorageUsedBytes);

        var history = await _db.HistoryEvents.FirstOrDefaultAsync(h => h.GroupId == groupId && h.Kind == HistoryEventKind.FileAdded);
        Assert.NotNull(history);
        Assert.Contains("lecture.pdf", history.Text);
    }

    [Fact]
    public async Task RenameAsync_StarostaCanRenameAnyFile_StudentOnlyOwnFile()
    {
        var groupId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var otherStudentId = Guid.NewGuid();
        var author = new AuthorSummary { Id = authorId, FirstName = "Автор", LastName = "Файлу", Role = UserRole.Student };

        var file = new StudShare.Api.Entities.File
        {
            GroupId = groupId,
            Name = "OriginalName",
            Extension = "pdf",
            Author = author
        };
        _db.Files.Add(file);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _fileService.RenameAsync(file.Id, new RenameFileRequest("HackedName"), otherStudentId, UserRole.Student));

        var renamedByAuthor = await _fileService.RenameAsync(file.Id, new RenameFileRequest("AuthorRenamed"), authorId, UserRole.Student);
        Assert.Equal("AuthorRenamed", renamedByAuthor.Name);

        var renamedByStarosta = await _fileService.RenameAsync(file.Id, new RenameFileRequest("StarostaRenamed"), otherStudentId, UserRole.Starosta);
        Assert.Equal("StarostaRenamed", renamedByStarosta.Name);
    }

    [Fact]
    public async Task TogglePinAsync_OnlyStarostaAllowed()
    {
        var groupId = Guid.NewGuid();
        var author = new AuthorSummary { Id = Guid.NewGuid(), FirstName = "Автор", LastName = "Файлу", Role = UserRole.Student };
        var file = new StudShare.Api.Entities.File
        {
            GroupId = groupId,
            Name = "ImportantFile",
            Extension = "pdf",
            Author = author
        };
        _db.Files.Add(file);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _fileService.TogglePinAsync(file.Id, UserRole.Student));

        var pinned = await _fileService.TogglePinAsync(file.Id, UserRole.Starosta);
        Assert.True(pinned.IsPinned);

        var unpinned = await _fileService.TogglePinAsync(file.Id, UserRole.Starosta);
        Assert.False(unpinned.IsPinned);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDecreaseStorageAndRemoveFile()
    {
        var groupId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var author = new AuthorSummary { Id = authorId, FirstName = "Автор", LastName = "Файлу", Role = UserRole.Student };

        var group = new Group { Id = groupId, Name = "ІП-44", Faculty = "ФІОТ", StorageUsedBytes = 2500 };
        _db.Groups.Add(group);

        var file = new StudShare.Api.Entities.File
        {
            GroupId = groupId,
            Name = "DeleteMe",
            Extension = "pdf",
            SizeBytes = 2500,
            Author = author
        };
        _db.Files.Add(file);
        await _db.SaveChangesAsync();

        var (_, remainingStorage) = await _fileService.DeleteAsync(file.Id, authorId, UserRole.Student);
        Assert.Equal(0, remainingStorage);

        var dbFile = await _db.Files.FindAsync(file.Id);
        Assert.Null(dbFile);
    }

    private static IFormFile CreateFormFile(string fileName, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();

        try
        {
            if (Directory.Exists(_env.ContentRootPath))
                Directory.Delete(_env.ContentRootPath, true);
        }
        catch { }
    }

    private class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "StudShare.Api";
        public string ContentRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "studshare_test_" + Guid.NewGuid());
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}

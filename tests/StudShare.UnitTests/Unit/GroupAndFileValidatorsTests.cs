using StudShare.Api.DTos;
using StudShare.Api.Validators;
using Xunit;

namespace StudShare.UnitTests.Unit;

public class GroupAndFileValidatorsTests
{
    private readonly CreateGroupRequestValidator _createGroupValidator = new();
    private readonly CreateFolderRequestValidator _createFolderValidator = new();
    private readonly RenameFileRequestValidator _renameFileValidator = new();
    private readonly InviteUserRequestValidator _inviteUserValidator = new();

    [Fact]
    public void CreateGroupRequest_Valid_ShouldPass()
    {
        var req = new CreateGroupRequest("ІП-44", "ФІОТ");
        var result = _createGroupValidator.Validate(req);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "ФІОТ")]
    [InlineData("ІП-44", "")]
    public void CreateGroupRequest_EmptyFields_ShouldFail(string name, string faculty)
    {
        var req = new CreateGroupRequest(name, faculty);
        var result = _createGroupValidator.Validate(req);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateFolderRequest_ValidRootFolder_ShouldPass()
    {
        var req = new CreateFolderRequest("Вища математика", null);
        var result = _createFolderValidator.Validate(req);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateFolderRequest_ValidNestedFolder_ShouldPass()
    {
        var parentId = Guid.NewGuid().ToString();
        var req = new CreateFolderRequest("Лекція 1", parentId);
        var result = _createFolderValidator.Validate(req);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateFolderRequest_InvalidParentGuid_ShouldFail()
    {
        var req = new CreateFolderRequest("Лекція 1", "not-a-guid");
        var result = _createFolderValidator.Validate(req);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "ParentId must be a valid GUID.");
    }

    [Fact]
    public void RenameFileRequest_EmptyName_ShouldFail()
    {
        var req = new RenameFileRequest("");
        var result = _renameFileValidator.Validate(req);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void InviteUserRequest_ValidEmail_ShouldPass()
    {
        var req = new InviteUserRequest("newstudent@kpi.ua");
        var result = _inviteUserValidator.Validate(req);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void InviteUserRequest_InvalidEmail_ShouldFail()
    {
        var req = new InviteUserRequest("not-an-email");
        var result = _inviteUserValidator.Validate(req);
        Assert.False(result.IsValid);
    }
}

using StudShare.Api.Entities;
using Xunit;

namespace StudShare.UnitTests.Unit;

public class RolePermissionsMatrixTests
{
    private static bool CanUpload(UserRole role, bool uploadAccess) =>
        role is UserRole.Starosta or UserRole.StudCurator || uploadAccess;

    private static bool CanPin(UserRole role) =>
        role == UserRole.Starosta;

    private static bool CanRenameOtherUserFile(UserRole role) =>
        role == UserRole.Starosta;

    private static bool CanDeleteOtherUserFile(UserRole role) =>
        role is UserRole.Starosta or UserRole.StudCurator;

    private static bool CanManageMembers(UserRole role) =>
        role == UserRole.StudCurator;

    [Theory]
    [InlineData(UserRole.Student, true, true)]
    [InlineData(UserRole.Student, false, false)]
    [InlineData(UserRole.Starosta, true, true)]
    [InlineData(UserRole.Starosta, false, true)]
    [InlineData(UserRole.StudCurator, true, true)]
    [InlineData(UserRole.StudCurator, false, true)]
    public void CanUpload_ShouldRespectPermissionsMatrix(UserRole role, bool uploadAccess, bool expected)
    {
        Assert.Equal(expected, CanUpload(role, uploadAccess));
    }

    [Theory]
    [InlineData(UserRole.Starosta, true)]
    [InlineData(UserRole.Student, false)]
    [InlineData(UserRole.StudCurator, false)]
    public void CanPin_OnlyStarostaAllowed(UserRole role, bool expected)
    {
        Assert.Equal(expected, CanPin(role));
    }

    [Theory]
    [InlineData(UserRole.Starosta, true)]
    [InlineData(UserRole.Student, false)]
    [InlineData(UserRole.StudCurator, false)]
    public void CanRenameOtherUserFile_OnlyStarostaAllowed(UserRole role, bool expected)
    {
        Assert.Equal(expected, CanRenameOtherUserFile(role));
    }

    [Theory]
    [InlineData(UserRole.Starosta, true)]
    [InlineData(UserRole.StudCurator, true)]
    [InlineData(UserRole.Student, false)]
    public void CanDeleteOtherUserFile_StarostaAndCuratorAllowed(UserRole role, bool expected)
    {
        Assert.Equal(expected, CanDeleteOtherUserFile(role));
    }

    [Theory]
    [InlineData(UserRole.StudCurator, true)]
    [InlineData(UserRole.Starosta, false)]
    [InlineData(UserRole.Student, false)]
    public void CanManageMembers_OnlyStudCuratorAllowed(UserRole role, bool expected)
    {
        Assert.Equal(expected, CanManageMembers(role));
    }
}

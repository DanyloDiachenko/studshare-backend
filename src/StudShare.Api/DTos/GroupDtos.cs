namespace StudShare.Api.DTos;

public record CreateGroupRequest(
    string Name,
    string Faculty
);

public record UpdateGroupRequest(
    string Name,
    string Faculty
);

public record DeleteGroupRequest(
    string ConfirmName
);

public record ChangeMemberRoleRequest(
    string Role
);

public record SetUploadAccessRequest(
    bool UploadAccess
);

public record GroupResponse(
    string Id,
    string Name,
    string Faculty,
    DateTime CreatedAt,
    long StorageUsedBytes,
    long StorageLimitBytes
);

public record GroupInfoResponse(
    string Id,
    string Name,
    string Faculty,
    DateTime CreatedAt,
    long StorageUsedBytes,
    long StorageLimitBytes,
    int MembersCount,
    int PendingInvitationsCount
);

public record GroupDetailsResponse(
    string Id,
    string Name,
    string Faculty,
    DateTime CreatedAt,
    long StorageUsedBytes,
    long StorageLimitBytes,
    List<UserResponse> Members,
    List<GroupInvitationResponse> Invitations,
    List<HistoryEventResponse> History,
    GroupContentResponse Content
);

public record GroupContentResponse(
    List<FolderResponse> Folders,
    List<FileResponse> Files
);

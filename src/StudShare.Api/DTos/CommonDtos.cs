using StudShare.Api.Entities;

namespace StudShare.Api.DTos;

public record AuthorSummaryResponse(
    string Id,
    string FirstName,
    string LastName,
    string? MiddleName,
    string Role
);

public record UserResponse(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string? MiddleName,
    string Role,
    string? GroupId,
    bool UploadAccess,
    DateTime JoinedAt,
    DateTime? InvitedAt,
    bool IsEmailVerified
);

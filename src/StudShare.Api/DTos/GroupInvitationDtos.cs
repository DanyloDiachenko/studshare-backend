namespace StudShare.Api.DTos;

public record InviteUserRequest(
    string Email
);

public record GroupInvitationResponse(
    string Id,
    string GroupId,
    string Email,
    AuthorSummaryResponse InvitedBy,
    DateTime InvitedAt,
    DateTime ExpiresAt
);

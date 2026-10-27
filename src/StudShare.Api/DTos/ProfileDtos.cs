namespace StudShare.Api.DTos;

public record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string? MiddleName,
    string Email
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string RepeatPassword
);

public record UpdateMemberRequest(
    string FirstName,
    string LastName,
    string? MiddleName
);

public record DeleteProfileRequest(
    string Password
);

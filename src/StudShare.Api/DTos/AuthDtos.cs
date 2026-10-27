namespace StudShare.Api.DTos;

public record SignUpRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? MiddleName
);

public record SignInRequest(
    string Email,
    string Password
);

public record ConfirmEmailRequest(
    string Email,
    string Code
);

public record ForgotPasswordRequest(
    string Email
);

public record ResetPasswordRequest(
    string Email,
    string Code,
    string NewPassword
);

public record AuthResponse(
    string Token
);

public record ProfileResponse(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string? MiddleName,
    string Role,
    string? GroupId,
    bool UploadAccess,
    DateTime JoinedAt
);

public record ErrorResponse(
    string Message
);

using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using StudShare.Api.DTos;
using StudShare.Api.Services;

namespace StudShare.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (
            [FromBody] SignUpRequest req,
            IValidator<SignUpRequest> validator,
            AuthService auth) =>
        {
            var result = await validator.ValidateAsync(req);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            try
            {
                var (token, user) = await auth.RegisterAsync(req);
                return Results.Created("/api/auth/me", new { accessToken = token.Token, user });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new ErrorResponse(ex.Message));
            }
        })
        .WithSummary("Register")
        .WithDescription("Creates a new user. If there is an active invitation for this email, the user is automatically joined to the group.");

        group.MapPost("/login", async (
            [FromBody] SignInRequest req,
            IValidator<SignInRequest> validator,
            AuthService auth) =>
        {
            var result = await validator.ValidateAsync(req);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            try
            {
                var (token, user) = await auth.LoginAsync(req);
                return Results.Ok(new { accessToken = token.Token, user });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        })
        .WithSummary("Login")
        .WithDescription("Verifies password and returns a JWT token and user profile.");

        group.MapPost("/forgot-password", async (
            [FromBody] ForgotPasswordRequest req,
            IValidator<ForgotPasswordRequest> validator,
            EmailVerificationService emailVerification) =>
        {
            var result = await validator.ValidateAsync(req);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            await emailVerification.SendPasswordResetAsync(req.Email);
            return Results.Ok(new { message = "If this email exists, a reset code has been sent." });
        })
        .WithSummary("Forgot password")
        .WithDescription("Sends a 6-digit reset code to the email address. Always returns 200 OK to avoid revealing whether an email is registered.");

        group.MapPost("/reset-password", async (
            [FromBody] ResetPasswordRequest req,
            IValidator<ResetPasswordRequest> validator,
            EmailVerificationService emailVerification) =>
        {
            var result = await validator.ValidateAsync(req);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            var success = await emailVerification.ResetPasswordAsync(req.Email, req.Code, req.NewPassword);
            if (!success)
                return Results.BadRequest(new ErrorResponse("Invalid or expired code."));

            return Results.Ok(new { success = true });
        })
        .WithSummary("Reset password")
        .WithDescription("Validates the 6-digit code from email and sets a new password. Code is valid for 15 minutes.");

        group.MapPost("/send-verification", async (
            [FromBody] ForgotPasswordRequest req,
            IValidator<ForgotPasswordRequest> validator,
            EmailVerificationService emailVerification) =>
        {
            var result = await validator.ValidateAsync(req);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            await emailVerification.SendConfirmationAsync(req.Email);
            return Results.Ok(new { message = "Verification code sent." });
        })
        .WithSummary("Send email verification code")
        .WithDescription("Generates and sends a 6-digit email confirmation code. Any previous code for this email is invalidated.");

        group.MapPost("/verify-email", async (
            [FromBody] ConfirmEmailRequest req,
            IValidator<ConfirmEmailRequest> validator,
            EmailVerificationService emailVerification) =>
        {
            var result = await validator.ValidateAsync(req);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            var success = await emailVerification.VerifyEmailConfirmationAsync(req.Email, req.Code);
            if (!success)
                return Results.BadRequest(new ErrorResponse("Invalid or expired code."));

            return Results.Ok(new { success = true });
        })
        .WithSummary("Verify email")
        .WithDescription("Accepts a 6-digit code and marks the email as verified (`isEmailVerified = true`).");

        group.MapGet("/me", async (CurrentUserService currentUser, AuthService auth) =>
        {
            var user = await auth.GetMeAsync(currentUser.UserId);
            return Results.Ok(user);
        })
        .WithSummary("Current user")
        .WithDescription("Returns the profile of the authenticated user based on JWT.")
        .RequireAuthorization();
    }
}

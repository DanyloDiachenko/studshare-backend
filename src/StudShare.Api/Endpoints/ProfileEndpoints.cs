using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudShare.Api.Data;
using StudShare.Api.DTos;
using StudShare.Api.Entities;
using StudShare.Api.Services;

namespace StudShare.Api.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile").WithTags("Profile").RequireAuthorization();

        group.MapPatch("/", async (
            [FromBody] UpdateProfileRequest req,
            IValidator<UpdateProfileRequest> validator,
            CurrentUserService currentUser,
            AppDbContext db) =>
        {
            var result = await validator.ValidateAsync(req);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            var user = await currentUser.GetUserAsync();
            var emailTaken = await db.Users.AnyAsync(u =>
                u.Email == req.Email.ToLowerInvariant() && u.Id != user.Id);
            if (emailTaken)
                return Results.Conflict(new ErrorResponse("Email already taken."));

            user.FirstName = req.FirstName;
            user.LastName = req.LastName;
            user.MiddleName = req.MiddleName;
            user.Email = req.Email.ToLowerInvariant();
            await db.SaveChangesAsync();

            return Results.Ok(AuthService.MapUser(user));
        })
        .WithSummary("Update profile")
        .WithDescription("Updates the current user's first name, last name, middle name and email. Validates email uniqueness.");

        group.MapDelete("/", async (
            [FromBody] DeleteProfileRequest req,
            IValidator<DeleteProfileRequest> validator,
            CurrentUserService currentUser,
            HistoryService historyService,
            AppDbContext db) =>
        {
            var result = await validator.ValidateAsync(req);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            var user = await currentUser.GetUserAsync();

            if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return Results.BadRequest(new ErrorResponse("Password is incorrect."));

            if (user.Role == UserRole.StudCurator && user.GroupId is not null)
                return Results.Conflict(new ErrorResponse("Transfer ownership or delete the group before deleting your account."));

            if (user.GroupId is not null)
            {
                var actor = new AuthorSummary
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    MiddleName = user.MiddleName,
                    Role = user.Role,
                };
                await historyService.LogAsync(user.GroupId.Value, HistoryEventKind.MemberRemoved, actor, "Left the group (deleted account)");
            }

            await db.EmailVerificationCodes
                .Where(c => c.Email == user.Email)
                .ExecuteDeleteAsync();

            db.Users.Remove(user);
            await db.SaveChangesAsync();

            return Results.Ok(new { success = true });
        })
        .WithSummary("Delete account")
        .WithDescription("Permanently deletes the current user's account. Requires password confirmation. StudCurator must delete or transfer group ownership first.");

        group.MapPost("/change-password", async (
            [FromBody] ChangePasswordRequest req,
            IValidator<ChangePasswordRequest> validator,
            CurrentUserService currentUser) =>
        {
            var result = await validator.ValidateAsync(req);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            var user = await currentUser.GetUserAsync();

            if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
                return Results.BadRequest(new ErrorResponse("Current password is incorrect."));

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            return Results.Ok(new { success = true });
        })
        .WithSummary("Change password")
        .WithDescription("Verifies the current password, validates newPassword and repeatPassword match, updates the hash.");
    }
}

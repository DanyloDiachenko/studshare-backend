using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using StudShare.Api.DTos;
using StudShare.Api.Entities;
using StudShare.Api.Services;

namespace StudShare.Api.Endpoints;

public static class MembersEndpoints
{
    public static void MapMembersEndpoints(this IEndpointRouteBuilder app)
    {
        var members = app.MapGroup("/api/members").WithTags("Members").RequireAuthorization();
        var invitations = app.MapGroup("/api/invitations").WithTags("Invitations").RequireAuthorization();

        members.MapGet("/", async (CurrentUserService currentUser, MemberService memberService) =>
        {
            var user = await currentUser.GetUserAsync();
            if (user.GroupId is null)
                return Results.BadRequest(new ErrorResponse("User is not in a group."));

            var (memberList, invitationList) = await memberService.GetMembersAsync(user.GroupId.Value);
            return Results.Ok(new { members = memberList, invitations = invitationList });
        })
        .WithSummary("List members and invitations")
        .WithDescription("Returns all group members and active invitations in a single request.");

        members.MapPatch("/{userId}/upload-access", async (
            Guid userId,
            [FromBody] SetUploadAccessRequest req,
            IValidator<SetUploadAccessRequest> validator,
            CurrentUserService currentUser,
            MemberService memberService) =>
        {
            if (currentUser.UserRole is not (UserRole.Starosta or UserRole.StudCurator))
                return Results.Forbid();

            var result = await validator.ValidateAsync(req);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            var user = await currentUser.GetUserAsync();
            if (user.GroupId is null)
                return Results.BadRequest(new ErrorResponse("User is not in a group."));

            try
            {
                var actor = await currentUser.GetActorAsync();
                var updated = await memberService.SetUploadAccessAsync(user.GroupId.Value, userId, req, actor);
                return Results.Ok(updated);
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new ErrorResponse(ex.Message)); }
            catch (KeyNotFoundException)         { return Results.NotFound(); }
        })
        .WithSummary("Set upload access")
        .WithDescription("Enables or disables `uploadAccess` for a member. Starosta or StudCurator.");

        members.MapPatch("/{userId}", async (
            Guid userId,
            [FromBody] UpdateMemberRequest req,
            IValidator<UpdateMemberRequest> validator,
            CurrentUserService currentUser,
            MemberService memberService) =>
        {
            if (currentUser.UserRole != UserRole.StudCurator)
                return Results.Forbid();

            var result = await validator.ValidateAsync(req);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            var user = await currentUser.GetUserAsync();
            if (user.GroupId is null)
                return Results.BadRequest(new ErrorResponse("User is not in a group."));

            try
            {
                var updated = await memberService.UpdateMemberAsync(user.GroupId.Value, userId, req);
                return Results.Ok(updated);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        })
        .WithSummary("Update member")
        .WithDescription("Updates a member's first name, last name, and middle name. StudCurator only.");

        members.MapPost("/{userId}/assign-starosta", async (
            Guid userId,
            CurrentUserService currentUser,
            MemberService memberService) =>
        {
            if (currentUser.UserRole != UserRole.StudCurator)
                return Results.Forbid();

            var user = await currentUser.GetUserAsync();
            if (user.GroupId is null)
                return Results.BadRequest(new ErrorResponse("User is not in a group."));

            try
            {
                var actor = await currentUser.GetActorAsync();
                var result = await memberService.AssignStarostaAsync(user.GroupId.Value, userId, actor);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        })
        .WithSummary("Assign starosta")
        .WithDescription("Assigns a member as starosta, demoting the previous one. StudCurator only. Records a HistoryEvent (Member).");

        members.MapDelete("/{userId}", async (
            Guid userId,
            CurrentUserService currentUser,
            MemberService memberService) =>
        {
            if (currentUser.UserRole != UserRole.StudCurator)
                return Results.Forbid();

            var user = await currentUser.GetUserAsync();
            if (user.GroupId is null)
                return Results.BadRequest(new ErrorResponse("User is not in a group."));

            try
            {
                var actor = await currentUser.GetActorAsync();
                var result = await memberService.RemoveMemberAsync(
                    user.GroupId.Value, userId, currentUser.UserId, actor);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new ErrorResponse(ex.Message)); }
            catch (KeyNotFoundException)         { return Results.NotFound(); }
        })
        .WithSummary("Remove member")
        .WithDescription("Removes a member from the group. StudCurator cannot remove themselves. Records a HistoryEvent (Member).");

        // --- Invitations ---

        invitations.MapPost("/", async (
            [FromBody] InviteUserRequest req,
            IValidator<InviteUserRequest> validator,
            CurrentUserService currentUser,
            InvitationService invitationService) =>
        {
            if (currentUser.UserRole != UserRole.StudCurator)
                return Results.Forbid();

            var result = await validator.ValidateAsync(req);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            var user = await currentUser.GetUserAsync();
            if (user.GroupId is null)
                return Results.BadRequest(new ErrorResponse("User is not in a group."));

            try
            {
                var actor = await currentUser.GetActorAsync();
                var invitation = await invitationService.InviteAsync(user.GroupId.Value, req, actor);
                return Results.Created($"/api/invitations/{invitation.Id}", invitation);
            }
            catch (InvalidOperationException ex) { return Results.Conflict(new ErrorResponse(ex.Message)); }
        })
        .WithSummary("Invite user")
        .WithDescription("Sends an invitation to the given email. Returns 409 if email is already in the group or has an active invitation. StudCurator only.");

        invitations.MapPost("/{id}/resend", async (
            Guid id,
            CurrentUserService currentUser,
            InvitationService invitationService) =>
        {
            if (currentUser.UserRole != UserRole.StudCurator)
                return Results.Forbid();

            var user = await currentUser.GetUserAsync();
            if (user.GroupId is null)
                return Results.BadRequest(new ErrorResponse("User is not in a group."));

            try
            {
                var updated = await invitationService.ResendAsync(id, user.GroupId.Value);
                return Results.Ok(updated);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        })
        .WithSummary("Resend invitation")
        .WithDescription("Updates `invitedAt` and resends the invitation email. StudCurator only.");

        invitations.MapDelete("/{id}", async (
            Guid id,
            CurrentUserService currentUser,
            InvitationService invitationService) =>
        {
            if (currentUser.UserRole != UserRole.StudCurator)
                return Results.Forbid();

            var user = await currentUser.GetUserAsync();
            if (user.GroupId is null)
                return Results.BadRequest(new ErrorResponse("User is not in a group."));

            try
            {
                var result = await invitationService.CancelAsync(id, user.GroupId.Value);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        })
        .WithSummary("Cancel invitation")
        .WithDescription("Deletes an invitation. StudCurator only.");
    }
}

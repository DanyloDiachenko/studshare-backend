using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using StudShare.Api.DTos;
using StudShare.Api.Entities;
using StudShare.Api.Services;

namespace StudShare.Api.Endpoints;

public static class GroupEndpoints
{
    public static void MapGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/groups").WithTags("Groups").RequireAuthorization();

        group.MapPost("/", async (
            [FromBody] CreateGroupRequest req,
            IValidator<CreateGroupRequest> validator,
            CurrentUserService currentUser,
            GroupService groupService) =>
        {
            var result = await validator.ValidateAsync(req);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            try
            {
                var created = await groupService.CreateAsync(req, currentUser.UserId);
                return Results.Created($"/api/groups/current", created);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new ErrorResponse(ex.Message));
            }
        })
        .WithSummary("Create group")
        .WithDescription("Creates a new group and makes the current user its StudCurator. Returns 409 if the user is already in a group.");

        group.MapGet("/current", async (CurrentUserService currentUser, GroupService groupService) =>
        {
            var user = await currentUser.GetUserAsync();
            if (user.GroupId is null)
                return Results.BadRequest(new ErrorResponse("User is not in a group."));

            var result = await groupService.GetCurrentAsync(user.GroupId.Value);
            return Results.Ok(result);
        })
        .WithSummary("Get group info")
        .WithDescription("Returns group header: name, faculty, storage limit, member and invitation counts. Available to all members.");

        group.MapPatch("/current", async (
            [FromBody] UpdateGroupRequest req,
            IValidator<UpdateGroupRequest> validator,
            CurrentUserService currentUser,
            GroupService groupService) =>
        {
            if (currentUser.UserRole != UserRole.StudCurator)
                return Results.Forbid();

            var result = await validator.ValidateAsync(req);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            var user = await currentUser.GetUserAsync();
            if (user.GroupId is null)
                return Results.BadRequest(new ErrorResponse("User is not in a group."));

            var actor = await currentUser.GetActorAsync();
            var updated = await groupService.UpdateAsync(user.GroupId.Value, req, actor);
            return Results.Ok(updated);
        })
        .WithSummary("Update group")
        .WithDescription("Updates the group name and faculty. StudCurator only. Records a HistoryEvent (Group).");

        group.MapDelete("/current", async (
            [FromBody] DeleteGroupRequest req,
            IValidator<DeleteGroupRequest> validator,
            CurrentUserService currentUser,
            GroupService groupService) =>
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
                await groupService.DeleteAsync(user.GroupId.Value, req);
                return Results.Ok(new { success = true });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .WithSummary("Delete group")
        .WithDescription("Deletes the group and all related data (files, folders, invitations, history). Requires confirmation via `confirmName`. StudCurator only.");
    }
}

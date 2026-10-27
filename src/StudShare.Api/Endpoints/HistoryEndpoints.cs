using StudShare.Api.DTos;
using StudShare.Api.Services;

namespace StudShare.Api.Endpoints;

public static class HistoryEndpoints
{
    public static void MapHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/history").WithTags("History").RequireAuthorization();

        group.MapGet("/", async (
            string? category,
            CurrentUserService currentUser,
            HistoryService historyService) =>
        {
            var user = await currentUser.GetUserAsync();
            if (user.GroupId is null)
                return Results.BadRequest(new ErrorResponse("User is not in a group."));

            var events = await historyService.GetHistoryAsync(user.GroupId.Value, category);
            return Results.Ok(events);
        })
        .WithSummary("Event feed")
        .WithDescription("Returns group events filtered by category (`File`, `Folder`, `Group`, `Member`). Without filter — all events.");
    }
}

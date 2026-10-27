namespace StudShare.Api.DTos;

public record HistoryEventResponse(
    string Id,
    string GroupId,
    string Kind,
    AuthorSummaryResponse Actor,
    string Text,
    DateTime CreatedAt
);

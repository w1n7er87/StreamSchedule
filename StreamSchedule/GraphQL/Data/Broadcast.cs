namespace StreamSchedule.GraphQL.Data;

public record Broadcast(
    Game? Game,
    string? ID,
    DateTime? StartedAt,
    string? Title
);

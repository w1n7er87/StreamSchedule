namespace StreamSchedule.GraphQL.Data;

public record Channel(
    ChattersInfo? Chatters,
    int? FounderBadgeAvailability,
    HypeTrain? HypeTrain
);

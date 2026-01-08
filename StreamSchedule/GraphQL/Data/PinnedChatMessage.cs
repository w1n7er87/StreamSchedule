namespace StreamSchedule.GraphQL.Data;

public record PinnedChatMessage(User? PinnedBy, Message? PinnedMessage);

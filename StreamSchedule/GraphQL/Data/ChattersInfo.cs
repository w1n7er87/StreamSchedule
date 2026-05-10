namespace StreamSchedule.GraphQL.Data;

public record ChattersInfo(int? Count, Chatter?[]? Viewers, Chatter?[]? Vips, Chatter?[]? Chatbots);

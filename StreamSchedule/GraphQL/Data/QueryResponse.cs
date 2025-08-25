namespace StreamSchedule.GraphQL.Data;

public record QueryResponse(
    UserDoesNotExist? UserResultByLogin,
    UserDoesNotExist? UserResultByID,
    Stream? Stream,
    Emote? Emote,
    Message? Message,
    User? User,
    bool? IsUsernameAvailable);

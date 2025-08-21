namespace StreamSchedule.GraphQL.Data;

public record GetUserResult(User? User, GetUserErrorReason? Reason, bool? IsUsernameAvailable);

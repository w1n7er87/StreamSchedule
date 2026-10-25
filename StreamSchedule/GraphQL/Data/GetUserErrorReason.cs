namespace StreamSchedule.GraphQL.Data;

public enum GetUserErrorReason
{
    UNKNOWN = 0,
    DEACTIVATED,
    DMCA,
    TOS_INDEFINITE,
    TOS_TEMPORARY,
}

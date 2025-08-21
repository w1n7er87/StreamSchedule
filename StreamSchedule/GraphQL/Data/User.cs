namespace StreamSchedule.GraphQL.Data;

public record User
(
    string? Login,
    string? Id,
    string? ChatColor,
    string? PrimaryColorHex,
    FollowerConnection? Followers,
    UserRoles? Roles,
    Channel? Channel,
    Broadcast? LastBroadcast,
    BroadcastSettings? BroadcastSettings,
    Stream? Stream,
    ChatSettings? ChatSettings,
    DateTime? CreatedAt,
    DateTime? DeletedAt,
    DateTime? UpdatedAt
);

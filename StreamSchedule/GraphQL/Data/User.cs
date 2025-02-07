namespace StreamSchedule.GraphQL.Data;

public class User
{
    public string? Login { get; set; }
    public string? Id { get; set; }
    public string? ChatColor { get; set; }
    public string? PrimaryColorHex { get; set; }
    public FollowerConnection? Followers { get; set; }
    public UserRoles? Roles { get; set; }
    public Channel? Channel { get; set; }
    public Broadcast? LastBroadcast { get; set; }
    public BroadcastSettings? BroadcastSettings { get; set; }
    public Stream? Stream { get; set; }
    public ChatSettings? ChatSettings { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

namespace StreamSchedule.GraphQL.Data;

public class User
{
    public string? Login { get; set; }
    public Channel? Channel { get; set; }
    public Broadcast? LastBroadcast { get;set; }
    public Stream? Stream { get; set; }
    public ChatSettings? ChatSettings { get; set; }
}

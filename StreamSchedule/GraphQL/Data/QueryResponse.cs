namespace StreamSchedule.GraphQL.Data;

public class QueryResponse
{
    public Stream? Stream { get; set; }
    public Emote? Emote { get; set; }
    public Message? Message { get; set; }
    public User? User { get; set; }
}

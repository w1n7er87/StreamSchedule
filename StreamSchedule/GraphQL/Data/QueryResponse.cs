namespace StreamSchedule.GraphQL.Data;

public class QueryResponse
{
    public UserDoesNotExist? UserResultByLogin { get; set; }
    public UserDoesNotExist? UserResultByID { get; set; }
    public Stream? Stream { get; set; }
    public Emote? Emote { get; set; }
    public Message? Message { get; set; }
    public User? User { get; set; }
    public bool? IsUsernameAvailable { get; set; }
}

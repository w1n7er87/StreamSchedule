namespace StreamSchedule.Data.Models;

public class User
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public List<string>? PreviousUsernames { get; set; }
    public Privileges privileges { get; set; } = Privileges.None;
    public int MessagesOffline { get; set; }
    public int MessagesOnline { get; set; }

}

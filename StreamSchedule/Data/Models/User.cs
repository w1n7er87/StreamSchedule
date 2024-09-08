namespace StreamSchedule.Data.Models;

public class User
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public List<string>? PreviousUsernames { get; set; }
    public Privileges privileges { get; set; } = Privileges.None;
}

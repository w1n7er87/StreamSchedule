namespace StreamSchedule.Data.Models;

public class User
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public List<string>? PreviousUsernames { get; set; }
    public Privileges privileges { get; set; } = Privileges.None;
    public int MessagesOffline { get; set; }
    public int MessagesOnline { get; set; }

    internal static User SyncToDb(User u, DatabaseContext context)
    {
        User? uDb = context.Users.Find(u.Id);
        if (uDb is null)
        {
            context.Users.Add(u);
            uDb = u;
            context.SaveChanges();
        }
        else
        {
            if (uDb.Username != u.Username)
            {
                uDb.PreviousUsernames ??= [];
                uDb.PreviousUsernames.Add(uDb.Username!);
                uDb.Username = u.Username;
                context.SaveChanges();
            }
        }
        return uDb;
    }

    internal static void AddMessagesCounter(User u, DatabaseContext context, int online = 0, int offline = 0)
    {
        u.MessagesOnline += online;
        u.MessagesOffline += offline;
        context.SaveChanges();
    }

    internal static bool TryGetUser(string username, out User user, string? id = null)
    {
        username = username.ToLower().Replace("@", "");

        if (string.IsNullOrEmpty(username)) { user = new User(); return false; }

        User? u = BotCore.DBContext.Users.FirstOrDefault(x => (id == null) ? x.Username == username : x.Id == int.Parse(id));

        if (u is null) { user = new User(); return false; }

        user = u;
        return true;
    }
}

namespace StreamSchedule.Data.Models;

public class User
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public List<string>? PreviousUsernames { get; set; }
    public Privileges Privileges { get; set; } = Privileges.None;
    public int MessagesOffline { get; set; }
    public int MessagesOnline { get; set; }

    internal static User SyncToDb(string userID, string username, TwitchLib.Client.Enums.UserType usertype, DatabaseContext context)
    {
        int userIDNumber = int.Parse(userID);

        User? uDb = context.Users.Find(userIDNumber);
        if (uDb is null)
        {
            User u = new()
            {
                Id = userIDNumber,
                Username = username,
                Privileges = usertype > TwitchLib.Client.Enums.UserType.Viewer ? Privileges.Mod : Privileges.None,
            };

            context.Users.Add(u);
            uDb = u;
        }
        else
        {
            if (uDb.Username == username) return uDb;
            uDb.PreviousUsernames ??= [];
            uDb.PreviousUsernames.Add(uDb.Username!);
            uDb.Username = username;
        }

        context.SaveChanges();
        return uDb;
    }

    internal static void AddMessagesCounter(User u, int online = 0, int offline = 0)
    {
        u.MessagesOnline += online;
        u.MessagesOffline += offline;
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

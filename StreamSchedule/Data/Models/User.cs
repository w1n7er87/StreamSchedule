namespace StreamSchedule.Data.Models;

public class User
{
    public int Id { get; init; }
    public string? Username { get; set; }
    public List<string>? PreviousUsernames { get; set; }
    public Privileges Privileges { get; set; } = Privileges.None;
    public int MessagesOffline { get; set; }
    public int MessagesOnline { get; set; }

    internal static User SyncToDb(string userID, string username, bool isMod, bool isVip, DatabaseContext context)
    {
        int userIDNumber = int.Parse(userID);

        User? uDb = context.Users.Find(userIDNumber);
        if (uDb is null)
        {
            User u = new()
            {
                Id = userIDNumber,
                Username = username,
                Privileges = (isVip, isMod) switch { (true, _) => Privileges.Trusted, (_ , true) => Privileges.Mod, _ => Privileges.None},
            };

            context.Users.Add(u);
            context.SaveChanges();
            uDb = u;
        }
        else
        {
            uDb.Privileges = (isVip, isMod, uDb.Privileges) switch
            {
                (true, _, > Privileges.Banned and < Privileges.Uuh) => Privileges.Trusted,
                (_, true, > Privileges.Banned and < Privileges.Uuh) => Privileges.Mod,
                (_, _, > Privileges.Banned) => uDb.Privileges,
                _ => Privileges.Banned
            };

            if (uDb.Username == username) return uDb;
            uDb.PreviousUsernames ??= [];
            uDb.PreviousUsernames.Add(uDb.Username!);
            uDb.Username = username;
        }
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

        if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(id))
        {
            user = new User();
            return false;
        }

        User? u = BotCore.DBContext.Users.FirstOrDefault(x => id == null ? x.Username == username : x.Id == int.Parse(id));

        if (u is not null)
        {
            user = u;
            return true;
        }

        User? byOldName = BotCore.DBContext.Users
            .Where(x => x.PreviousUsernames != null)
            .FirstOrDefault(x => x.PreviousUsernames!.Contains(username));

        if (byOldName is not null)
        {
            user = byOldName;
            return true;
        }

        user = new User();
        return false;
    }
}

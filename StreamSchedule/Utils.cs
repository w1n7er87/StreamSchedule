using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule;

internal static class Utils
{
    internal static class Responses
    {
        internal static CommandResult Fail => new("NOIDONTTHINKSO ", false);
        internal static CommandResult Ok => new("ok ", false);
        internal static CommandResult Surprise => new("oh ", false);
    }

    internal static Privileges ParsePrivilege(string text)
    {
        return text.ToLower() switch
        {
            "ban" => Privileges.Banned,
            "ok" => Privileges.None,
            "trust" => Privileges.Trusted,
            "mod" => Privileges.Mod,
            _ => Privileges.None
        };
    }

    internal static string PrivilegeToString(Privileges p)
    {
        return p switch
        {
            Privileges.Banned => "banned",
            Privileges.None => "a regular",
            Privileges.Trusted => "a VIP",
            Privileges.Mod => "a mod MONKA ",
            Privileges.Uuh => "a uuh ",
            _ => "an alien "
        };
    }

    internal static User SyncToDb(User u, ref DatabaseContext context)
    {
        User? uDb = context.Users.Find(u.Id);
        if (uDb is null)
        {
            context.Users.Add(u);
            uDb = u;
        }
        else
        {
            if (uDb.Username != u.Username)
            {
                context.Users.Update(uDb);
                uDb.PreviousUsernames ??= [];
                uDb.PreviousUsernames.Add(uDb.Username!);
                uDb.Username = u.Username;
            }
        }
        context.SaveChanges();
        return uDb;
    }

    internal static void AddMessagesCounter(User u, ref DatabaseContext context, int online = 0, int offline = 0)
    {
        context.Users.Update(u);
        u.MessagesOnline += online;
        u.MessagesOffline += offline;
        context.SaveChanges();
    }

    internal static string RetrieveArguments(string[] args, string input, out Dictionary<string, string> usedArgs)
    {
        usedArgs = [];
        string[] split = input.Split(' ');

        foreach (string ss in split)
        {
            foreach (var arg in args)
            {
                if (ss.Contains($"-{arg}", StringComparison.InvariantCultureIgnoreCase))
                {
                    usedArgs[arg.ToLower()] = ss.Replace($"-{arg}", "");
                    input = input.Replace(ss, "", StringComparison.InvariantCultureIgnoreCase);
                }
            }
        }
        return input;
    }

    internal static bool TryGetUser(string username, out User user, string? id = null)
    {
        username = username.ToLower().Replace("@", "");

        if (string.IsNullOrEmpty(username)) { user = new User(); return false; }

        User? u = Body.dbContext.Users.SingleOrDefault(x => (id == null) ? x.Username == username : x.Id == int.Parse(id));

        if (u is null) { user = new User(); return false; }

        user = u;
        return true;
    }
}

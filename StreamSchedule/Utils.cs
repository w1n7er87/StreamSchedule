using StreamSchedule.Data;
using StreamSchedule.Data.Models;
using System.Text;

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
            _ => "an alien "
        };
    }

    internal static User SyncToDb(User u, ref DatabaseContext context)
    {
        User? uDb = context.Users.Find(u.Id);
        if (uDb == null)
        {
            context.Users.Add(u);
            uDb = u;
        }
        else
        {
            if (uDb.Username != u.Username)
            {
                context.Users.Update(uDb);
                if(uDb.PreviousUsernames == null) uDb.PreviousUsernames = [];
                uDb.PreviousUsernames!.Append(uDb.Username);
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

    internal static string RetrieveArguments(string[] args, string input, out List<string> usedArgs)
    {
        usedArgs = [];
        foreach (var arg in args)
        {
            if (input.Contains($"-{arg}", StringComparison.InvariantCultureIgnoreCase))
            {
                input = input.Replace($"-{arg}", "", StringComparison.InvariantCultureIgnoreCase);
                usedArgs.Add(arg.ToLower());
            }
        }
        return input;
    }
}

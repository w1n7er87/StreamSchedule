using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule;

internal static class Utils
{
    internal static class Responses
    {
        internal static string Fail => "NOIDONTTHINKSO ";
        internal static string Ok => "ok ";
        internal static string Surprise => "oh ";
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
                uDb.PreviousUsernames?.Append(uDb.Username);
                uDb.Username = u.Username;
            }
        }
        context.SaveChanges();
        return uDb;
    }
}

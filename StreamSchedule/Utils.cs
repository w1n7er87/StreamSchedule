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

    internal static bool TryGetUser(string username, out User user, string? id = null)
    {
        username = username.ToLower().Replace("@", "");

        if (string.IsNullOrEmpty(username)) { user = new User(); return false; }

        User? u = Body.dbContext.Users.FirstOrDefault(x => (id == null) ? x.Username == username : x.Id == int.Parse(id));

        if (u is null) { user = new User(); return false; }

        user = u;
        return true;
    }
}

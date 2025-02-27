using Microsoft.EntityFrameworkCore;
using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Top : Command
{
    internal override string Call => "top";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "get top chatters by messages sent offline(default)/online or by score/ratio";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Medium);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[] Arguments => ["online", "offline", "score", "ratio", "p", "d"];

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        _ = Commands.RetrieveArguments(Arguments, message.content, out Dictionary<string, string> args);
        CommandResult result = new();

        int page = 0;
        if (args.TryGetValue("p", out string? p))
        {
            int.TryParse(p, out page);
            page = Math.Max(page - 1, 0);
        }

        bool descending = false;
        if (args.TryGetValue("d", out _)) descending = true;

        if (args.TryGetValue("ratio", out _))
        {
            var topTen = BotCore.DBContext.Users.AsNoTracking().AsEnumerable().OrderByDescending(x => descending ? -Userscore.GetRatioAndScore(x).ratio : Userscore.GetRatioAndScore(x).ratio).ToList();
            int start = Math.Min(page * 10, topTen.Count - 10);
            topTen = topTen.GetRange(start, 10);
            for (int i = 0; i < topTen.Count; i++)
            {
                var user = topTen[i];
                result += $"{start + i + 1} {user.Username!.Insert(1, "󠀀")} {MathF.Round(Userscore.GetRatioAndScore(user).ratio, 3)} er ";
            }
        }
        else if (args.TryGetValue("score", out _))
        {
            var topTen = BotCore.DBContext.Users.AsNoTracking().AsEnumerable().OrderByDescending(x => descending ? -Userscore.GetRatioAndScore(x).score : Userscore.GetRatioAndScore(x).score).ToList();
            int start = Math.Min(page * 10, topTen.Count - 10);
            topTen = topTen.GetRange(start, 10);
            for (int i = 0; i < topTen.Count; i++)
            {
                var user = topTen[i];
                result += $"{start + i + 1} {user.Username!.Insert(1, "󠀀")} {MathF.Round(Userscore.GetRatioAndScore(user).score, 3)} er ";
            }
        }
        else if (args.TryGetValue("online", out _))
        {
            var topTen = BotCore.DBContext.Users.OrderByDescending(x => descending ? -x.MessagesOnline : x.MessagesOnline).AsNoTracking().ToList();
            int start = Math.Min(page * 10, topTen.Count - 10);
            topTen = topTen.GetRange(start, 10);
            for (int i = 0; i < topTen.Count; i++)
            {
                var user = topTen[i];
                result += $"{start + i + 1} {user.Username!.Insert(1, "󠀀")} {user.MessagesOnline} er ";
            }
        }
        else
        {
            var topTen = BotCore.DBContext.Users.OrderByDescending(x => descending ? -x.MessagesOffline : x.MessagesOffline).AsNoTracking().ToList();
            int start = Math.Min(page * 10, topTen.Count - 10);
            topTen = topTen.GetRange(start, 10);
            for (int i = 0; i < topTen.Count; i++)
            {
                var user = topTen[i];
                result += $"{start + i + 1} {user.Username!.Insert(1, "󠀀")} {user.MessagesOffline} er ";
            }
        }

        return Task.FromResult(result);
    }
}

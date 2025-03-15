using Microsoft.EntityFrameworkCore;
using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class Top : Command
{
    internal override string Call => "top";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "get top chatters by messages sent offline(default)/online or by score/ratio";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[] Arguments => ["online", "offline", "score", "ratio", "p", "d"];

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        _ = Commands.RetrieveArguments(Arguments, message.content, out Dictionary<string, string> args);
        CommandResult result = new();

        int page = args.TryGetValue("p", out string? p) ? int.TryParse(p, out int pp) ? Math.Max(pp - 1, 0) : 0 : 0;
        
        bool descending = args.TryGetValue("d", out _);

        if (args.TryGetValue("ratio", out _))
        {
            List<User> topTen = [.. BotCore.DBContext.Users.AsNoTracking().AsEnumerable().OrderByDescending(x => descending ? -Userscore.GetRatioAndScore(x).ratio : Userscore.GetRatioAndScore(x).ratio)];
            int start = Math.Min(page * 10, topTen.Count - 10);
            topTen = topTen.GetRange(start, 10);
            for (int i = 0; i < topTen.Count; i++)
            {
                User user = topTen[i];
                result += $"{start + i + 1} {user.Username!.Insert(1, "󠀀")} {MathF.Round(Userscore.GetRatioAndScore(user).ratio, 3)} er ";
            }
        }
        else if (args.TryGetValue("score", out _))
        {
            List<User> topTen = [.. BotCore.DBContext.Users.AsNoTracking().AsEnumerable().OrderByDescending(x => descending ? -Userscore.GetRatioAndScore(x).score : Userscore.GetRatioAndScore(x).score)];
            int start = Math.Min(page * 10, topTen.Count - 10);
            topTen = topTen.GetRange(start, 10);
            for (int i = 0; i < topTen.Count; i++)
            {
                User user = topTen[i];
                result += $"{start + i + 1} {user.Username!.Insert(1, "󠀀")} {MathF.Round(Userscore.GetRatioAndScore(user).score, 3)} er ";
            }
        }
        else if (args.TryGetValue("online", out _))
        {
            List<User> topTen = [.. BotCore.DBContext.Users.OrderByDescending(x => descending ? -x.MessagesOnline : x.MessagesOnline).AsNoTracking()];
            int start = Math.Min(page * 10, topTen.Count - 10);
            topTen = topTen.GetRange(start, 10);
            for (int i = 0; i < topTen.Count; i++)
            {
                User user = topTen[i];
                result += $"{start + i + 1} {user.Username!.Insert(1, "󠀀")} {user.MessagesOnline} er ";
            }
        }
        else
        {
            List<User> topTen = [.. BotCore.DBContext.Users.OrderByDescending(x => descending ? -x.MessagesOffline : x.MessagesOffline).AsNoTracking()];
            int start = Math.Min(page * 10, topTen.Count - 10);
            topTen = topTen.GetRange(start, 10);
            for (int i = 0; i < topTen.Count; i++)
            {
                User user = topTen[i];
                result += $"{start + i + 1} {user.Username!.Insert(1, "󠀀")} {user.MessagesOffline} er ";
            }
        }

        return Task.FromResult(result);
    }
}

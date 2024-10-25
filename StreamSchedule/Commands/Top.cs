using Microsoft.EntityFrameworkCore;
using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Top : Command
{
    internal override string Call => "top";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "get top chatters by messages sent offline(default)/online or by score ";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => ["online", "offline", "score"];

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        _ = Commands.RetrieveArguments(Arguments!, message.Message, out Dictionary<string, string> args);
        CommandResult result = new("");

        if (args.Count == 0 || args.TryGetValue("offline", out _))
        {
            var topTen = BotCore.DBContext.Users.OrderByDescending(x => x.MessagesOffline).AsNoTracking().Take(10).ToList();
            for (int i = 0; i < topTen.Count; i++)
            {
                var user = topTen[i];
                result += $"{i + 1} {user.Username!.Insert(1, "󠀀")} {user.MessagesOffline} ppL ";
            }
        }

        if (args.TryGetValue("score", out _))
        {
            var topTen = BotCore.DBContext.Users.AsEnumerable().OrderByDescending(x => Userscore.GetRatioAndScore(x).score).Take(10).ToList();
            for (int i = 0; i < topTen.Count; i++)
            {
                var user = topTen[i];
                result += $"{i + 1} {user.Username!.Insert(1, "󠀀")} {MathF.Round(Userscore.GetRatioAndScore(user).score, 3)} ppL ";
            }
        }

        if (args.TryGetValue("online", out _))
        {
            var topTen = BotCore.DBContext.Users.OrderByDescending(x => x.MessagesOnline).AsNoTracking().Take(10).ToList();
            for (int i = 0; i < topTen.Count; i++)
            {
                var user = topTen[i];
                result += $"{i + 1} {user.Username!.Insert(1, "󠀀")} {user.MessagesOnline} ppL ";
            }
        }

        return Task.FromResult(result);
    }
}

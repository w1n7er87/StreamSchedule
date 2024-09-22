using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Top : Command
{
    internal override string Call => "top";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "get top ";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => ["online", "offline", "score"];

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string text = Commands.RetrieveArguments(Arguments!, message.Message, out Dictionary<string, string> args);
        CommandResult result = new("");

        if (args.Count == 0 || args.TryGetValue("offline", out _))
        {
            var topTen = BotCore.DBContext.Users.OrderByDescending(x => x.MessagesOffline).Take(10).ToList();
            for (int i = 0; i < topTen.Count; i++)
            {
                var user = topTen[i];
                result += $"{i + 1} {user.Username}_ {user.MessagesOffline} Clap ";
            }
        }

        if (args.TryGetValue("score", out _))
        {
            var topTen = BotCore.DBContext.Users.AsEnumerable().OrderByDescending(x => Userscore.GetRatioAndScore(x).score).Take(10).ToList();
            for (int i = 0; i < topTen.Count; i++)
            {
                var user = topTen[i];
                result += $"{i + 1} {user.Username}_ {MathF.Round(Userscore.GetRatioAndScore(user).score, 3)} Clap ";
            }
        }

        if (args.TryGetValue("online", out _))
        {
            var topTen = BotCore.DBContext.Users.OrderByDescending(x => x.MessagesOnline).Take(10).ToList();
            for (int i = 0; i < topTen.Count; i++)
            {
                var user = topTen[i];
                result += $"{i + 1} {user.Username}_ {user.MessagesOnline} Clap ";
            }
        }

        return Task.FromResult(result);
    }
}

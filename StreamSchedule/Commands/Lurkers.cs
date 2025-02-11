using StreamSchedule.Data;
using StreamSchedule.GraphQL.Data;

namespace StreamSchedule.Commands;

internal class Lurkers : Command
{
    internal override string Call => "lurkers";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "get channel lurkers";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Minute);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[] Arguments => ["p"];

    internal override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string? target = Commands.RetrieveArguments(Arguments, message.content, out Dictionary<string, string> usedArgs).Split(' ')[0];

        if (!string.IsNullOrWhiteSpace(target) && target.StartsWith('@') && message.sender.Privileges >= Privileges.Trusted)
        {
            target = target.Replace("@", "");
            (int, Chatter?[]?) c = await BotCore.GQLClient.GetChattersCount(message.roomID, target);
            return new($"{c.Item1} lurkers in {target}'s chat uuh ");
        }

        (int, Chatter?[]?) count = await BotCore.GQLClient.GetChattersCount(message.roomID);
        string chatter = "";
        if (usedArgs.TryGetValue("p", out _))
        {
            if (count.Item2 is not null && count.Item2.Length != 0) chatter = $", and @{count.Item2[Random.Shared.Next(count.Item2.Length)]?.Login ?? BotCore.Client.TwitchUsername} is one of them";
        }
        return new($"{count.Item1} lurkers{chatter} uuh");
    }
}

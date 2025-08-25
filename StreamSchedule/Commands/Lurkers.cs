using StreamSchedule.Data;
using StreamSchedule.GraphQL;
using StreamSchedule.GraphQL.Data;

namespace StreamSchedule.Commands;

internal class Lurkers : Command
{
    public override string Call => "lurkers";
    public override Privileges Privileges => Privileges.None;
    public override string Help => "get channel lurkers";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.ThreeMinutes);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];

    public override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        (int, Chatter?[]?) count = await GraphQLClient.GetChattersCount(message.ChannelID);

        string chatter = count.Item2 is not null && count.Item2.Length != 0
            ? $", and @{count.Item2[Random.Shared.Next(count.Item2.Length)]?.Login ?? BotCore.ChatClient.TwitchUsername} is one of them"
            : "";

        return new($"{count.Item1} lurkers{chatter} uuh");
    }
}

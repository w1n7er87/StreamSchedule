using StreamSchedule.Data;
using StreamSchedule.GraphQL.Data;

namespace StreamSchedule.Commands;

class ChannelRules : Command
{
    internal override string Call => "rules";

    internal override Privileges MinPrivilege => Privileges.Banned;

    internal override string Help => "channel rules";

    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int) Cooldowns.Medium);

    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];

    internal override string[]? Arguments => null;

    internal override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        ChatSettings? settings = await BotCore.GQLClient.GetChatSettings(message.roomID);
        string links = settings?.BlockLinks switch
        {
            true => "no links ",
            _ => ""
        };

        return new($"{links}{string.Join(", ", settings?.Rules ?? ["no channel rules (or failed to fetch idk."])}");
    }
}

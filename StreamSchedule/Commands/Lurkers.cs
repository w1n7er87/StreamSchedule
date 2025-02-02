using StreamSchedule.Data;

namespace StreamSchedule.Commands;

class Lurkers : Command
{
    internal override string Call => "lurkers";

    internal override Privileges MinPrivilege => Privileges.None;

    internal override string Help => "get channel lurkers";

    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int) Cooldowns.Short);

    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];

    internal override string[]? Arguments => null;

    internal override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        int count = await BotCore.GQLClient.GetChattersCount(message.roomID);
        return new($"{count} lurkers uuh");
    }
}

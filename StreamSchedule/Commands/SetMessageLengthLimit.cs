using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class SetMessageLengthLimit : Command
{
    internal override string Call => "setlimit";
    internal override Privileges MinPrivilege => Privileges.Uuh;
    internal override string Help => "set character limit per message";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.None);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        int limit = int.TryParse(message.content.Split(' ')[0], out int r) ? int.Clamp(r, 10, 500) : 350;
        BotCore.MessageLengthLimit = limit;
        return Task.FromResult(Utils.Responses.Ok + $" {limit} it is.");
    }
}

using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class SetMessageLengthLimit : Command
{
    public override string Call => "setlimit";
    public override Privileges Privileges => Privileges.Uuh;
    public override string Help => "set character limit per message";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];

    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        int limit = int.TryParse(message.Content.Split(' ')[0], out int r) ? int.Clamp(r, 5, 500) : 350;
        BotCore.MessageLengthLimit = limit;
        return Task.FromResult(Utils.Responses.Ok + $" {limit} it is.");
    }
}

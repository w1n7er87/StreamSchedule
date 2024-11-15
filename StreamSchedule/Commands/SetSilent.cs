using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class SetSilent : Command
{
    internal override string Call => "silent";
    internal override Privileges MinPrivilege => Privileges.Uuh;
    internal override string Help => "toggle silent mode";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Short);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;
    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        BotCore.Silent = !BotCore.Silent;
        return Task.FromResult(new CommandResult());
    }
}
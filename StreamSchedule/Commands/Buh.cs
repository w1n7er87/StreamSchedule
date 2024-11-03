using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Buh : Command
{
    internal override string Call => "buh";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "buh or buhblunt with 1% chance.";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Medium);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        int a = new Random().Next(101);
        return Task.FromResult(new CommandResult(a == 69 ? "buhblunt " : "buh ", false));
    }
}

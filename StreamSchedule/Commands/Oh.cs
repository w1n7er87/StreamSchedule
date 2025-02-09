using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Oh : Command
{
    internal override string Call => "oh";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "oh or Tutoh with 1% chance.";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Medium);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        int a = new Random().Next(100);
        return Task.FromResult(new CommandResult(a == 69 ? "Tutoh " : "oh ", false));
    }
}

using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Oh : Command
{
    public override string Call => "oh";
    public override Privileges Privileges => Privileges.None;
    public override string Help => "oh or Tutoh with 1% chance";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];

    public override Task<CommandResult> Handle(UniversalMessageInfo message) => Task.FromResult(new CommandResult(Random.Shared.Next(100) == 69 ? "Tutoh " : "oh ", false));
}

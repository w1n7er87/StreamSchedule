using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class SetSilent : Command
{
    public override string Call => "silent";
    public override Privileges Privileges => Privileges.Mod;
    public override string Help => "toggle silent mode";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];

    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        BotCore.Silent = !BotCore.Silent;
        return Task.FromResult(new CommandResult());
    }
}

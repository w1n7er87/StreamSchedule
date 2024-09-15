using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Skin : Command
{
    internal override string Call => "skin";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "osu skin";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        return Task.FromResult(new CommandResult("dokidokilolixx 2018-06-10 "));
    }
}

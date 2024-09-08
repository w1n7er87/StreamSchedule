using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Skin : Command
{
    internal override string Call => "skin";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "osu skin";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(5);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];

    internal override Task<string> Handle(UniversalMessageInfo message)
    {
        return Task.FromResult("dokidokilolixx 2018-06-10 ");
    }
}

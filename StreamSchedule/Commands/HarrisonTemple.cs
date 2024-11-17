using StreamSchedule.Data;
using StreamSchedule.HarrisonTemple;

namespace StreamSchedule.Commands;

internal class HarrisonTemple : Command
{
    internal override string Call => "harrisontemple";
    internal override Privileges MinPrivilege => Privileges.Trusted;
    internal override string Help => "walk through Harrison Temple";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Medium);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;
    
    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        CommandResult result = new();

        Room room = HarrisonTempleGame.GenerateRoom();

        result += $"{room.view} {room.exp} exp, {room.reward} coins";
        
        return Task.FromResult(result);
    }
}

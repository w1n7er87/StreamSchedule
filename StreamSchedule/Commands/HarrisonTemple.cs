using StreamSchedule.Data;
using StreamSchedule.Data.Models;
using StreamSchedule.HarrisonTemple;

namespace StreamSchedule.Commands;

internal class HarrisonTemple : Command
{
    internal override string Call => "harrisontemple";
    internal override Privileges MinPrivilege => Privileges.Trusted;
    internal override string Help => "walk through the Harrison Temple";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Medium);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;
    
    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        CommandResult result = new();
//TODO: per-user daily limit, store daily run count in db, items, some sort of task that resets runs daily for everyone 
        Room room = HarrisonTempleGame.GenerateRoom();

        result += $"{room.view} {room.exp} exp, {room.reward} coins";
        HarrisonTempleStat? userStats =  BotCore.DBContext.HarrisonTempleStats.Find(message.Sender.Id);
        if (userStats is not null)
        {
            userStats.TotalExp += room.exp;
            userStats.TotalCoins += room.reward;
        }
        
        return Task.FromResult(result);
    }
}

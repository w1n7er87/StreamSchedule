using StreamSchedule.Data;

namespace StreamSchedule.Commands;

public class VedalPlush : Command
{
    internal override string Call => "plush";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "stayTuteled ";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int) Cooldowns.Medium);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;
    
    internal override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        var count = await BotCore.VedalPlushClient.GetPlushCount();
        if (count is null) return new CommandResult("no data", false, false);

        string isLive = (count.Node?.AvailableForSale ?? false) ? "is live" : "";
        return new CommandResult($"Vedal plush {isLive} and has sold {Math.Abs(count.Node?.Quantity ?? 0)}");
    }
}

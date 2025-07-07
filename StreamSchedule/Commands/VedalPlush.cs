using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class VedalPlush : Command
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

        if (!DateTime.TryParse(count.Node?.MetafieldsWithReference?.FirstOrDefault(x => x?.Type?.Equals("date_time") ?? false)?.Value ?? "", out DateTime endTime))
            endTime = DateTime.Now;
        endTime = TimeZoneInfo.ConvertTimeToUtc(endTime);
        
        TimeSpan span = endTime - DateTime.UtcNow;
        string time = $"{(span.Days != 0 ? span.Days + "d " : "")}{(span.Hours != 0 ? span.Hours + "h " : "")}{span:m'm 's's '}";

        string isLive = (count.Node?.AvailableForSale ?? false) ? "is live and" : "";
        return new CommandResult($"Vedal plush {isLive} has sold {Math.Abs(count.Node?.Quantity ?? 0)} . Only {time} left DinkDonk ", false);
    }
}

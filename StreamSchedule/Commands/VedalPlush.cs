using StreamSchedule.Data;
using StreamSchedule.VedalPlush;
using StreamSchedule.VedalPlush.Data;

namespace StreamSchedule.Commands;

internal class VedalPlush : Command
{
    public override string Call => "plush";
    public override Privileges Privileges => Privileges.Mod;
    public override string Help => "stayTuteled ";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Medium);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];

    public override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        Response? count = await VedalPlushClient.GetPlushCount();
        if (count is null) return new("no data", false, false);

        if (!DateTime.TryParse(
                count.Node?.MetafieldsWithReference?.FirstOrDefault(x => x?.Type?.Equals("date_time") ?? false)
                    ?.Value ?? "", out DateTime endTime))
            endTime = DateTime.Now;
        endTime = TimeZoneInfo.ConvertTimeToUtc(endTime);

        TimeSpan span = endTime - DateTime.UtcNow;
        string time =
            $"{(span.Days != 0 ? span.Days + "d " : "")}{(span.Hours != 0 ? span.Hours + "h " : "")}{span:m'm 's's '}";

        string isLive = count.Node?.AvailableForSale ?? false ? "is live and" : "";
        return new($"Vedal plush {isLive} has sold {Math.Abs(count.Node?.Quantity ?? 0)} . Only {time} left DinkDonk ",
            false);
    }
}

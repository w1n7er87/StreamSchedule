using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class ClearDay : Command
{
    public override string Call => "clearday";
    public override Privileges Privileges => Privileges.Mod;
    public override string Help => "clear schedule for the given day: [date] (d-M-yy or dd-MM-yy)";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];

    private readonly string[] _inputPatterns = ["d-M-yy", "dd-MM-yy", "d-M-yyyy", "dd-MM-yyyy"];

    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string[] split = message.Content.Split(' ');

        if (split.Length < 1 || !DateTime.TryParseExact(split[0], _inputPatterns, null, System.Globalization.DateTimeStyles.AssumeLocal, out DateTime temp))
            return Task.FromResult(Utils.Responses.Fail + "bad date ");

        Data.Models.Stream? interest = BotCore.DBContext.Streams.FirstOrDefault(s => s.StreamDate == DateOnly.FromDateTime(temp));

        if (interest == null) return Task.FromResult(new CommandResult("Nothing on that day.", false));

        BotCore.DBContext.Streams.Remove(interest);
        BotCore.DBContext.SaveChanges();
        return Task.FromResult(Utils.Responses.Ok);
    }
}

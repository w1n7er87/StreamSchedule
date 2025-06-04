using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class ClearDay : Command
{
    internal override string Call => "clearday";
    internal override Privileges MinPrivilege => Privileges.Mod;
    internal override string Help => "clear schedule for the given day: [date] (d-M-yy or dd-MM-yy)";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    private readonly string[] _inputPatterns = ["d-M-yy", "dd-MM-yy", "d-M-yyyy", "dd-MM-yyyy"];

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string[] split = message.content.Split(' ');

        if (split.Length < 1 || !DateTime.TryParseExact(split[0], _inputPatterns, null, System.Globalization.DateTimeStyles.AssumeLocal, out DateTime temp))
            return Task.FromResult(Utils.Responses.Fail + "bad date ");
        
        Data.Models.Stream? interest = BotCore.DBContext.Streams.FirstOrDefault(s => s.StreamDate == DateOnly.FromDateTime(temp));

        if (interest == null) return Task.FromResult(new CommandResult("Nothing on that day.", false));
        
        BotCore.DBContext.Streams.Remove(interest);
        BotCore.DBContext.SaveChanges();
        return Task.FromResult(Utils.Responses.Ok);
        
    }
}

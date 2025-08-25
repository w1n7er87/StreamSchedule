using StreamSchedule.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Stream = StreamSchedule.Data.Models.Stream;

namespace StreamSchedule.Commands;

internal class GetStream : Command
{
    public override string Call => "stream";
    public override Privileges Privileges => Privileges.None;
    public override string Help => "time until next stream on the schedule";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[] Arguments => ["h", "min", "s", "mic", "ms", "ns", "d", "y"];
    public override List<string> Aliases { get; set; } = [];

    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        Commands.RetrieveArguments(Arguments, message.Content, out Dictionary<string, string> usedArgs);

        Stream? next = BotCore.DBContext.Streams.Where(x => x.StreamDate >= DateOnly.FromDateTime(DateTime.UtcNow))
            .AsNoTracking()
            .AsEnumerable()
            .OrderBy(x => new DateTime(x.StreamDate, x.StreamTime))
            .FirstOrDefault(x => new DateTime(x.StreamDate, x.StreamTime) >= DateTime.UtcNow);

        if (next is null) return Task.FromResult(new CommandResult("There are no more streams scheduled DinkDonk "));

        DateTime fullDate = new DateTime(next.StreamDate, next.StreamTime).ToLocalTime();
        TimeSpan span = fullDate - DateTime.Now;
        string time = $"{(span.Days != 0 ? span.Days + "d " : "")}{(span.Hours != 0 ? span.Hours + "h " : "")}{span:m'm 's's '}";

        if (usedArgs.TryGetValue("y", out _)) time = (span.TotalDays / 365.0).ToString(CultureInfo.InvariantCulture) + " years";
        if (usedArgs.TryGetValue("d", out _)) time = span.TotalDays.ToString(CultureInfo.InvariantCulture) + " days";
        if (usedArgs.TryGetValue("h", out _)) time = span.TotalHours.ToString(CultureInfo.InvariantCulture) + " hours";
        if (usedArgs.TryGetValue("min", out _)) time = span.TotalMinutes.ToString(CultureInfo.InvariantCulture) + " minutes";
        if (usedArgs.TryGetValue("s", out _)) time = span.TotalSeconds.ToString(CultureInfo.InvariantCulture) + " seconds";
        if (usedArgs.TryGetValue("ms", out _)) time = span.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) + " milliseconds";
        if (usedArgs.TryGetValue("mic", out _)) time = span.TotalMicroseconds.ToString(CultureInfo.InvariantCulture) + " microseconds";
        if (usedArgs.TryGetValue("ns", out _)) time = span.TotalNanoseconds.ToString(CultureInfo.InvariantCulture) + " nanoseconds";

        return Task.FromResult(new CommandResult($"Next stream is in {time} : {next.StreamTitle}"));
    }
}

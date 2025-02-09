using StreamSchedule.Data;
using System.Globalization;

namespace StreamSchedule.Commands;

internal class GetStream : Command
{
    internal override string Call => "stream";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "time until next stream on the schedule.";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[] Arguments => ["h", "min", "s", "mic", "ms", "ns", "d", "y"];

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        Commands.RetrieveArguments(Arguments, message.content, out Dictionary<string, string> usedArgs);

        var next = BotCore.DBContext.Streams.Where(x => x.StreamDate >= DateOnly.FromDateTime(DateTime.UtcNow))
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

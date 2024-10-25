using Microsoft.EntityFrameworkCore;
using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class GetStream : Command
{
    internal override string Call => "stream";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "time until next stream on the schedule.";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        var next = BotCore.DBContext.Streams.Where(x => x.StreamDate >= DateOnly.FromDateTime(DateTime.UtcNow))
            .ToList()
            .OrderBy(x => new DateTime(x.StreamDate, x.StreamTime))
            .FirstOrDefault(x => new DateTime(x.StreamDate, x.StreamTime) >= DateTime.UtcNow);

        if (next is null)
        {
            return Task.FromResult(new CommandResult("There are no more streams scheduled DinkDonk "));
        }

        DateTime fullDate = new DateTime(next.StreamDate, next.StreamTime).ToLocalTime();
        TimeSpan span = fullDate - DateTime.Now;
        return Task.FromResult(new CommandResult($"Next stream is in {(span.Days != 0 ? span.Days + "d " : "")}{(span.Hours != 0 ? span.Hours + "h " : "")}{span:m'm 's's '} : {next.StreamTitle}"));
    }
}

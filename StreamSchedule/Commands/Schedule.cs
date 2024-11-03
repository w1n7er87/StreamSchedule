using Microsoft.EntityFrameworkCore;
using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Schedule : Command
{
    internal override string Call => "schedule";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "show streams for the next week per day.";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        DateOnly inAWeek = DateOnly.FromDateTime(DateTime.Now + TimeSpan.FromDays(7));
        var streams = BotCore.DBContext.Streams.Where(s => s.StreamDate >= DateOnly.FromDateTime(DateTime.Now) && s.StreamDate <= inAWeek).AsNoTracking();

        if (!streams.Any()) { return Task.FromResult(new CommandResult("The schedule is empty. SadCat ")); }

        CommandResult response = new("");
        foreach (var stream in streams)
        {
            response += new DateTime(stream.StreamDate, stream.StreamTime).ToString("ddd") + ": " + stream.StreamTitle?[..Math.Min(50, stream.StreamTitle.Length)] + ".  ";
        }
        return Task.FromResult(response);
    }
}

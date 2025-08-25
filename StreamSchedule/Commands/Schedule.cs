using Microsoft.EntityFrameworkCore;
using StreamSchedule.Data;
using System.Text;
using Stream = StreamSchedule.Data.Models.Stream;

namespace StreamSchedule.Commands;

internal class Schedule : Command
{
    public override string Call => "schedule";
    public override Privileges Privileges => Privileges.None;
    public override string Help => "show streams for the next week per day";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];

    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        IQueryable<Stream> streams = BotCore.DBContext.Streams.Where(s => s.StreamDate >= DateOnly.FromDateTime(DateTime.Now)).AsNoTracking();

        if (!streams.Any()) return Task.FromResult(new CommandResult("The schedule is empty. SadCat "));

        DateOnly inAWeek = DateOnly.FromDateTime(DateTime.Now + TimeSpan.FromDays(7));
        StringBuilder sb = new();

        string currentOrLatestTZ = DateTime.Now.ToString("zzz");

        foreach (Stream stream in streams)
        {
            DateTime streamDate = new DateTime(stream.StreamDate, stream.StreamTime).ToLocalTime();

            string streamTZ = streamDate.ToString("zzz");
            if (!currentOrLatestTZ.Equals(streamTZ))
            {
                currentOrLatestTZ = streamTZ;
                sb.Append($"(UTC{DateTime.Now:zzz})");
            }

            string when = stream.StreamDate > inAWeek
                ? $"{streamDate:(MMM/dd) ddd HH:mm}"
                : $"{streamDate:ddd HH:mm}";
            sb.Append($"{when} : {stream.StreamTitle?[..Math.Min(50, stream.StreamTitle.Length)]} . ");
        }

        return Task.FromResult(new CommandResult(sb.Append($"(UTC{currentOrLatestTZ})")));
    }
}

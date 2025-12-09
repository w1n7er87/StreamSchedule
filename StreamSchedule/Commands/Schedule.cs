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
        bool dateRequested = Utils.TryParseLocalDate(message.Content.Split(" ").FirstOrDefault(), out DateTime requestedDate);
        
        DateTime startDate = dateRequested ? requestedDate : DateTime.Now;
        
        IQueryable<Stream> streams = BotCore.DBContext.Streams.Where(s => s.StreamDate >= DateOnly.FromDateTime(startDate)).AsNoTracking();

        DateOnly inAWeek = DateOnly.FromDateTime(startDate + TimeSpan.FromDays(7));
        
        if (dateRequested) streams = streams.Where(s => s.StreamDate <= inAWeek);

        if (!streams.Any()) return Task.FromResult(new CommandResult("The schedule is empty. SadCat "));
        
        StringBuilder sb = new();

        string currentOrLatestTZ = startDate.ToString("zzz");

        foreach (Stream stream in streams)
        {
            DateTime streamDate = new DateTime(stream.StreamDate, stream.StreamTime).ToLocalTime();

            string streamTZ = streamDate.ToString("zzz");
            if (!currentOrLatestTZ.Equals(streamTZ))
            {
                currentOrLatestTZ = streamTZ;
                sb.Append($"(UTC{startDate:zzz})");
            }

            string when = (stream.StreamDate > inAWeek, dateRequested) switch
            {
                (_, true) or (true, false) => $"{streamDate:(MMM/dd) ddd HH:mm}",
                (false, false) => $"{streamDate:ddd HH:mm}"
            };

            sb.Append($"{when} : {stream.StreamTitle?[..Math.Min(50, stream.StreamTitle.Length)]} . ");
        }

        return Task.FromResult(new CommandResult(sb.Append($"(UTC{currentOrLatestTZ})")));
    }
}

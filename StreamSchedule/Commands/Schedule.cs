using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Schedule : Command
{
    internal override string Call => "schedule";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "show streams for the next week per day.";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(1.1);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];

    internal override Task<string> Handle(UniversalMessageInfo message)
    {
        DateOnly inAWeek = DateOnly.FromDateTime(DateTime.Now + TimeSpan.FromDays(8));
        var streams = Body.dbContext.Streams.Where(s => s.StreamDate >= DateOnly.FromDateTime(DateTime.Now) && s.StreamDate <= inAWeek);
        string response = "";
        foreach (var stream in streams)
        {
            response += new DateTime(stream.StreamDate, stream.StreamTime).ToString("ddd") + ": " + stream.StreamTitle?[..Math.Min(25, stream.StreamTitle.Length)] + ".  ";
        }
        return Task.FromResult(response);
    }
}

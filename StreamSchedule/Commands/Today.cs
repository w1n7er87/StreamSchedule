using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Today : Command
{
    internal override string Call => "today";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "check if there is a stream today.";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(2);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];

    internal override Task<string> Handle(UniversalMessageInfo message)
    {
        Data.Models.Stream? today = Body.dbContext.Streams.SingleOrDefault(s => s.StreamDate == DateOnly.FromDateTime(DateTime.Now));
        if (today == null || new DateTime(today.StreamDate, today.StreamTime) < DateTime.Now)
        {
            return Task.FromResult("There is no stream today DinkDonk ");
        }
        else
        {
            DateTime fullDate = new DateTime(today.StreamDate, today.StreamTime);
            TimeSpan span = fullDate - DateTime.Now;
            return Task.FromResult($"The {today.StreamTitle} is in {Math.Floor(span.TotalHours).ToString() + span.ToString("'h 'm'm 's's'")} DinkDonk ");
        }
    }
}

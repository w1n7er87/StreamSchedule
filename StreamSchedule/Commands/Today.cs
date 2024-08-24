using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Today : Command
{
    internal override string Call => "today";

    internal override Privileges MinPrivilege => Privileges.None;

    internal override string Handle(UniversalMessageInfo message)
    {
        Data.Models.Stream? today = Body.dbContext.Streams.SingleOrDefault(s => s.StreamDate == DateOnly.FromDateTime(DateTime.Now));
        if (today == null || new DateTime(today.StreamDate, today.StreamTime) < DateTime.Now)
        {
            return "There is no stream today DinkDonk ";
        }
        else
        {
            return $"There is a {today.StreamTitle} stream today DinkDonk ";
        }
    }
}

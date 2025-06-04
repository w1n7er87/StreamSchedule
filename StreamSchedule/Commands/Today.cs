using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Today : Command
{
    internal override string Call => "today";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "check if there is a stream today";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        Data.Models.Stream? today = BotCore.DBContext.Streams.FirstOrDefault(s => s.StreamDate == DateOnly.FromDateTime(DateTime.UtcNow));
        if (today == null || new DateTime(today.StreamDate, today.StreamTime) < DateTime.UtcNow) return Task.FromResult(new CommandResult("There is no stream today DinkDonk "));

        DateTime fullDate = new DateTime(today.StreamDate, today.StreamTime).ToLocalTime();
        TimeSpan span = fullDate - DateTime.Now;
        return Task.FromResult(new CommandResult($"The {today.StreamTitle} is in {(span.Days != 0 ? span.Days + "d " : "")}{(span.Hours != 0 ? span.Hours + "h " : "")}{span:m'm 's's '}DinkDonk "));
    }
}

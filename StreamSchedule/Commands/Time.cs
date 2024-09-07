using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Time : Command
{
    internal override string Call => "time";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "current time.";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(1);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];

    internal override string Handle(UniversalMessageInfo message)
    {
        return DateTime.Now.ToString("ddd HH:mm:ss") + " Latege ";
    }
}

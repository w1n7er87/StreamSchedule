using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal abstract class Command
{
    internal abstract string Call { get; }
    internal abstract Privileges MinPrivilege { get; }
    internal abstract string Help { get; }
    internal abstract TimeSpan Cooldown { get; }
    internal abstract Dictionary<string, DateTime> LastUsedOnChannel { get; set; }

    internal abstract Task<string> Handle(UniversalMessageInfo message);
}

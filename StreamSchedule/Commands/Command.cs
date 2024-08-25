using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal abstract class Command
{
    internal abstract string Call { get; }
    internal abstract Privileges MinPrivilege { get; }
    internal abstract string Help { get; }

    internal abstract string Handle(UniversalMessageInfo message);
}

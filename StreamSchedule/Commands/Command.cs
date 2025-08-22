using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal abstract class Command : ICommand
{
    public abstract string Call { get; }
    public abstract Privileges Privileges { get; }
    public abstract string Help { get; }
    public abstract TimeSpan Cooldown { get; }
    public abstract Dictionary<string, DateTime> LastUsedOnChannel { get; }
    public abstract string[]? Arguments { get; }
    public abstract List<string> Aliases { get; set; }

    public abstract Task<CommandResult> Handle(UniversalMessageInfo message);
}

using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal interface ICommand
{
    internal string Call { get; }
    internal Privileges Privileges { get; }
    internal string Help { get; }
    internal TimeSpan Cooldown { get; }
    internal Dictionary<int, Cooldown> PersonalCooldowns { get; }
    internal string[]? Arguments { get; }
    internal List<string> Aliases { get; }

    internal Task<CommandResult> Handle(UniversalMessageInfo message);
}

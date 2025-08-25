using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Helps : Command
{
    public override string Call => "helps";
    public override Privileges Privileges => Privileges.None;
    public override string Help => "show command help: [command name]";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];

    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string[] split = message.Content.Split(' ');
        if (split.Length < 1) return Task.FromResult(new CommandResult(Help));

        string requestedCommand = split[0].ToLower();

        ICommand? c = Commands.AllCommands.FirstOrDefault(x =>
            x.Call.Equals(requestedCommand, StringComparison.OrdinalIgnoreCase) ||
            x.Aliases.Contains(requestedCommand));
        
        if (c is null) return Task.FromResult(new CommandResult(Help));

        string aliases = c.Aliases.Count != 0 ? $"( {string.Join(" , ", c.Aliases)} )" : "";
        string args = c.Arguments is not null ? $"args: {string.Join(", ", c.Arguments)}" : "";
        string cd = $"cooldown: {c.Cooldown.TotalSeconds}s";
        return Task.FromResult(new CommandResult($"{c.Call} {aliases} {c.Help}.{args} {cd} "));
    }
}

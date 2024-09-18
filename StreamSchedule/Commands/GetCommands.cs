using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class GetCommands : Command
{
    internal override string Call => "commands";
    internal override Privileges MinPrivilege => Privileges.Banned;
    internal override string Help => "show list of commands available to you. ";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Medium);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string response = "";
        foreach (var c in Body.CurrentCommands)
        {
            if (c == null) { continue; }
            if (c.MinPrivilege <= message.Privileges) { response += c.Call + ", "; }
        }
        return Task.FromResult(new CommandResult(response[..^2] + ". "));
    }
}

using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class GetCommands : Command
{
    internal override string Call => "commands";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "show list of commands available to you. ";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(1.1);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];

    internal override Task<string> Handle(UniversalMessageInfo message)
    {
        string response = "";
        foreach (var c in Body.currentCommands)
        {
            if (c == null) { continue; }
            if (c.MinPrivilege <= message.Privileges) { response += c.Call + ", "; }
        }
        return Task.FromResult(response[..^2] + ". ");
    }
}

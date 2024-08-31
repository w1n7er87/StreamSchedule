using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class GetCommands : Command
{
    internal override string Call => "commands";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "show list of commands available to you. ";

    internal override string Handle(UniversalMessageInfo message)
    {
        string response = "";
        foreach (var c in Body.currentCommands)
        {
            if (c == null) { continue; }
            if (c.MinPrivilege <= message.Privileges) { response += c.Call + ", "; }
        }
        return response[..^2] + ". ";
    }
}

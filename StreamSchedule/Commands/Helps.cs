using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Helps : Command
{
    internal override string Call => "helps";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "show command help: [command name] ";

    internal override string Handle(UniversalMessageInfo message)
    {
        string[] split = message.Message.Split(' ');
        if (split.Length < 2 ) { return this.Help; }
        foreach (Command? c in Body.currentCommands)
        {
            if (c == null) { continue; }
            if (split[1] == c.Call) {return c.Help; }
        }
        return this.Help;
    }
}

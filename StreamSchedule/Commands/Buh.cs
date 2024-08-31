using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Buh : Command
{
    internal override string Call => "buh";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "buh or buhblunt with 1% chance.";

    internal override string Handle(UniversalMessageInfo message)
    {
        int a = new Random().Next(101);
        return a == 69 ? "buhblunt " : "buh ";
    }
}

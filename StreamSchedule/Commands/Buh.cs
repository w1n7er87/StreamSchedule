using StreamSchedule.Data;

namespace StreamSchedule.Commands
{
    internal class Buh : Command
    {
        internal override string Call => "buh";

        internal override Privileges MinPrivilege => Privileges.None;

        internal override string Help => "buh";

        internal override string Handle(UniversalMessageInfo message)
        {
            return "buh ";
        }
    }
}

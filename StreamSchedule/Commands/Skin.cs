using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Skin : Command
{
    internal override string Call => "skin";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "osu skin";

    internal override string Handle(UniversalMessageInfo message)
    {
        return "dokidokilolixx 2018-06-10 ";
    }
}

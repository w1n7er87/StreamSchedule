using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class SetSlow : Command
{
    internal override string Call => "setslow";
    internal override Privileges MinPrivilege => Privileges.Mod;
    internal override string Help => "set time, during which, any incoming messages are ignored by bot after it sends a message: [time] (in ms, optional)";

    internal override string Handle(UniversalMessageInfo message)
    {
        string[] split = message.Message.Split(' ');
        if (int.TryParse(split[1], out int value))
        {
            Body.messagesIgnoreDelayMS = Math.Min(100, value);
        }
        else
        {
            Body.messagesIgnoreDelayMS = 350;
        }
        return Utils.Responses.Ok;
    }
}

using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Kill : Command
{
    internal override string Call => "kill";

    internal override Privileges MinPrivilege => Privileges.Mod;

    private async Task KillTask(TimeSpan delay)
    {
        await Task.Delay(delay);
        Environment.Exit(0);
    }

    internal override string Handle(UniversalMessageInfo message)
    {
        string[] split = message.Message.Split(' ');
        TimeSpan delay = split.Length > 1 ? TimeSpan.FromSeconds(int.Parse(message.Message.Split(' ')[1])) : TimeSpan.FromSeconds(1);
        Task.Run(() => KillTask(delay));
        return "buhbye ";
    }
}

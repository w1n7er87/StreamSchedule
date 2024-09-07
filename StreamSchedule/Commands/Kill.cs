using StreamSchedule.Data;

namespace StreamSchedule.Commands;

/// <summary>
/// surely there will no problems if bot shuts down during db operations (actually Clueless )
/// </summary>
internal class Kill : Command
{
    internal override string Call => "kill";
    internal override Privileges MinPrivilege => Privileges.Mod;
    internal override string Help => "kill the bot: [time] (in seconds, optional)";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(1.1);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];

    private async Task KillTask(TimeSpan delay)
    {
        await Task.Delay(delay);
        Environment.Exit(0);
    }

    internal override string Handle(UniversalMessageInfo message)
    {
        string[] split = message.Message.Split(' ');
        TimeSpan delay = split.Length > 1 ? TimeSpan.FromSeconds(Math.Min(1, int.Parse(message.Message.Split(' ')[1]))) : TimeSpan.FromSeconds(1);
        Task.Run(() => KillTask(delay));
        return "buhbye ";
    }
}
